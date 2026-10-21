using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using ServiceDesk.Domain.Abstractions;
using ServiceDesk.Domain.Entities;
using ServiceDesk.Domain.Enums;
using ServiceDesk.Infrastructure.Data;
using ServiceDesk.Infrastructure.Sla;

namespace ServiceDesk.Infrastructure.Portal;

public sealed class PortalService : IPortalService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly ApplicationDbContext _dbContext;
    private readonly ICurrentRequester _currentRequester;
    private readonly ITenantContext _tenantContext;
    private readonly TimeProvider _timeProvider;
    private readonly ITicketSlaService _ticketSlaService;

    public PortalService(
        ApplicationDbContext dbContext,
        ICurrentRequester currentRequester,
        ITenantContext tenantContext,
        TimeProvider timeProvider,
        ITicketSlaService ticketSlaService)
    {
        _dbContext = dbContext;
        _currentRequester = currentRequester;
        _tenantContext = tenantContext;
        _timeProvider = timeProvider;
        _ticketSlaService = ticketSlaService;
    }

    public async Task<IReadOnlyList<CatalogueItem>> GetPublishedCatalogueAsync(
        string? search,
        CancellationToken cancellationToken = default)
    {
        var items = _dbContext.CatalogueItems
            .Where(item => item.Status == CatalogueItemStatus.Published)
            .Include(item => item.FulfilmentGroup)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            if (term.Length > 200)
            {
                throw new ArgumentException("Catalogue search must be 200 characters or fewer.", nameof(search));
            }

            items = items.Where(item =>
                item.Title.Contains(term) || item.Description.Contains(term));
        }

        return await items
            .OrderBy(item => item.Title)
            .Take(100)
            .ToListAsync(cancellationToken);
    }

    public Task<CatalogueItem?> GetPublishedCatalogueItemAsync(
        Guid itemId,
        CancellationToken cancellationToken = default) =>
        _dbContext.CatalogueItems
            .Include(item => item.FulfilmentGroup)
            .SingleOrDefaultAsync(item =>
                item.ItemId == itemId && item.Status == CatalogueItemStatus.Published,
                cancellationToken);

    public async Task<IReadOnlyList<KnowledgeArticle>> SearchKnowledgeAsync(
        string? search,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(search))
        {
            return Array.Empty<KnowledgeArticle>();
        }

        var term = search.Trim();
        if (term.Length > 200)
        {
            throw new ArgumentException("Knowledge search must be 200 characters or fewer.", nameof(search));
        }

        return await _dbContext.KnowledgeArticles
            .Where(article => article.Status == KnowledgeArticleStatus.Published
                && (article.Title.Contains(term) || article.Content.Contains(term)))
            .OrderBy(article => article.Title)
            .Take(50)
            .ToListAsync(cancellationToken);
    }

    public async Task<Ticket> SubmitIncidentAsync(
        string title,
        string description,
        TicketPriority priority,
        CancellationToken cancellationToken = default)
    {
        ValidateText(title, nameof(title), 300);
        ValidateText(description, nameof(description), 20_000);
        ValidateRequester();
        ValidatePriority(priority);

        var customer = await GetActiveCustomerAsync(cancellationToken);
        var triageGroup = await GetTriageGroupAsync(cancellationToken);
        var now = _timeProvider.GetUtcNow();
        var ticketId = Guid.NewGuid();
        var ticket = new Ticket
        {
            TicketId = ticketId,
            TenantId = _tenantContext.TenantId,
            Reference = GenerateReference("INC", ticketId),
            Type = TicketType.Incident,
            Title = title.Trim(),
            Description = description.Trim(),
            Status = TicketStatus.Triage,
            Priority = priority,
            CustomerId = customer.CustomerId,
            GroupId = triageGroup.GroupId,
            CreatedAt = now
        };
        ticket.GroupHistory.Add(CreateInitialGroupHistory(
            ticket, triageGroup.GroupId, _currentRequester.UserId, now));

        _dbContext.Tickets.Add(ticket);
        await _dbContext.SaveChangesAsync(cancellationToken);
        await _ticketSlaService.EvaluateAsync(ticket.TicketId, cancellationToken);
        return ticket;
    }

    public async Task<Ticket> SubmitServiceRequestAsync(
        Guid itemId,
        IReadOnlyDictionary<string, string?> answers,
        bool eligibilityConfirmed,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(answers);
        ValidateRequester();

        var item = await GetPublishedCatalogueItemAsync(itemId, cancellationToken);
        if (item is null)
        {
            throw new CatalogueItemUnavailableException(
                "This catalogue item is no longer available for new submissions.");
        }

        if (!eligibilityConfirmed)
        {
            throw new ArgumentException("Please confirm that you meet the eligibility requirements.",
                nameof(eligibilityConfirmed));
        }

        var validatedAnswers = ValidateAnswers(item.FormSchemaJson, answers);
        var customer = await GetActiveCustomerAsync(cancellationToken);
        var group = await _dbContext.ResolverGroups
            .SingleOrDefaultAsync(candidate =>
                candidate.GroupId == item.FulfilmentGroupId && candidate.Active,
                cancellationToken);
        if (group is null)
        {
            throw new InvalidOperationException(
                "The fulfilment group for this catalogue item is not currently active.");
        }

        var now = _timeProvider.GetUtcNow();
        var snapshot = new RequestSnapshot(
            ItemId: item.ItemId,
            ItemTitle: item.Title,
            FormSchemaJson: item.FormSchemaJson,
            SubmittedAt: now,
            Answers: validatedAnswers);
        var ticketId = Guid.NewGuid();
        var ticket = new Ticket
        {
            TicketId = ticketId,
            TenantId = _tenantContext.TenantId,
            Reference = GenerateReference("SR", ticketId),
            Type = TicketType.ServiceRequest,
            Title = item.Title,
            Description = $"Service request submitted through the self-service portal for \"{item.Title}\".",
            Status = group.IsTriageGroup ? TicketStatus.Triage : TicketStatus.New,
            Priority = TicketPriority.Normal,
            CustomerId = customer.CustomerId,
            GroupId = group.GroupId,
            CreatedAt = now,
            CatalogueItemId = item.ItemId,
            CatalogueItemTitleSnapshot = item.Title,
            RequestAnswersJson = JsonSerializer.Serialize(snapshot, JsonOptions)
        };
        ticket.GroupHistory.Add(CreateInitialGroupHistory(
            ticket, group.GroupId, _currentRequester.UserId, now));

        _dbContext.Tickets.Add(ticket);
        await _dbContext.SaveChangesAsync(cancellationToken);
        await _ticketSlaService.EvaluateAsync(ticket.TicketId, cancellationToken);
        return ticket;
    }

    private async Task<CustomerOrganisation> GetActiveCustomerAsync(
        CancellationToken cancellationToken)
    {
        var customerId = _currentRequester.CustomerId;
        if (customerId == Guid.Empty)
        {
            throw new InvalidOperationException("The current requester has no customer organisation.");
        }

        var customer = await _dbContext.CustomerOrganisations
            .SingleOrDefaultAsync(candidate => candidate.CustomerId == customerId
                && candidate.Status == CustomerOrganisationStatus.Active,
                cancellationToken);

        return customer ?? throw new InvalidOperationException(
            "The current requester is not associated with an active customer organisation.");
    }

    private async Task<ResolverGroup> GetTriageGroupAsync(CancellationToken cancellationToken)
    {
        var group = await _dbContext.ResolverGroups
            .SingleOrDefaultAsync(candidate => candidate.IsTriageGroup && candidate.Active,
                cancellationToken);

        return group ?? throw new InvalidOperationException(
            "No active triage group is configured for this tenant.");
    }

    private void ValidateRequester()
    {
        if (_currentRequester.UserId == Guid.Empty)
        {
            throw new InvalidOperationException("The current requester identity is not available.");
        }
    }

    private static TicketGroupHistory CreateInitialGroupHistory(
        Ticket ticket,
        Guid groupId,
        Guid actorId,
        DateTimeOffset now) =>
        new()
        {
            HistoryId = Guid.NewGuid(),
            TenantId = ticket.TenantId,
            TicketId = ticket.TicketId,
            FromGroupId = null,
            ToGroupId = groupId,
            ChangedBy = actorId,
            ChangedAt = now
        };

    private static IReadOnlyDictionary<string, string> ValidateAnswers(
        string schemaJson,
        IReadOnlyDictionary<string, string?> answers)
    {
        IReadOnlyList<CatalogueFormField>? fields;
        if (schemaJson.Length > 64_000 || answers.Count > 100)
        {
            throw new InvalidOperationException("The catalogue item has an invalid request form configuration.");
        }

        try
        {
            fields = JsonSerializer.Deserialize<IReadOnlyList<CatalogueFormField>>(schemaJson, JsonOptions);
        }
        catch (JsonException exception)
        {
            throw new InvalidOperationException("The catalogue item has an invalid request form configuration.",
                exception);
        }

        if (fields is null || fields.Count > 100)
        {
            throw new InvalidOperationException("The catalogue item has an invalid request form configuration.");
        }

        var result = new Dictionary<string, string>(StringComparer.Ordinal);
        var seenNames = new HashSet<string>(StringComparer.Ordinal);
        foreach (var field in fields)
        {
            if (!IsValidFieldDefinition(field) || !seenNames.Add(field.Name))
            {
                throw new InvalidOperationException("The catalogue item has an invalid request form configuration.");
            }

            var rawValue = answers.TryGetValue(field.Name, out var supplied) ? supplied : null;
            var value = rawValue?.Trim() ?? string.Empty;
            if (field.Required && value.Length == 0)
            {
                throw new ArgumentException($"\"{field.Label}\" is required.", nameof(answers));
            }

            if (value.Length > field.MaxLength)
            {
                throw new ArgumentException(
                    $"\"{field.Label}\" must be {field.MaxLength} characters or fewer.", nameof(answers));
            }

            if (field.Type == "select" && value.Length > 0 && !field.Options.Contains(value, StringComparer.Ordinal))
            {
                throw new ArgumentException($"Select a valid option for \"{field.Label}\".", nameof(answers));
            }

            if (field.Type == "email" && value.Length > 0
                && !System.Net.Mail.MailAddress.TryCreate(value, out _))
            {
                throw new ArgumentException($"Enter a valid email address for \"{field.Label}\".",
                    nameof(answers));
            }

            result.Add(field.Name, value);
        }

        if (answers.Keys.Any(name => !seenNames.Contains(name)))
        {
            throw new ArgumentException("The request includes unrecognized form fields.", nameof(answers));
        }

        return result;
    }

    private static bool IsValidFieldDefinition(CatalogueFormField field)
    {
        if (field is null
            || field.Options is null
            || string.IsNullOrWhiteSpace(field.Name)
            || !Regex.IsMatch(field.Name, "^[A-Za-z][A-Za-z0-9_]{0,49}$")
            || string.IsNullOrWhiteSpace(field.Label)
            || field.Label.Length > 100
            || field.MaxLength is < 1 or > 5000)
        {
            return false;
        }

        return field.Type switch
        {
            "text" or "textarea" => field.Options.Count == 0,
            "email" => field.Options.Count == 0 && field.MaxLength <= 320,
            "select" => field.Options.Count is > 0 and <= 50
                && field.Options.All(option => !string.IsNullOrWhiteSpace(option) && option.Length <= 200),
            _ => false
        };
    }

    private static void ValidateText(string value, string parameterName, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Trim().Length > maxLength)
        {
            throw new ArgumentException($"A value containing 1 to {maxLength} characters is required.",
                parameterName);
        }
    }

    private static void ValidatePriority(TicketPriority priority)
    {
        if (!Enum.IsDefined(priority))
        {
            throw new ArgumentOutOfRangeException(nameof(priority));
        }
    }

    private static string GenerateReference(string prefix, Guid ticketId) =>
        $"{prefix}-{ticketId:N}";

    private sealed record RequestSnapshot(
        Guid ItemId,
        string ItemTitle,
        string FormSchemaJson,
        DateTimeOffset SubmittedAt,
        IReadOnlyDictionary<string, string> Answers);
}
