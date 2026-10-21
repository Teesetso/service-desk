using Microsoft.EntityFrameworkCore;
using ServiceDesk.Domain.Abstractions;
using ServiceDesk.Domain.Entities;
using ServiceDesk.Domain.Enums;
using ServiceDesk.Infrastructure.Data;

namespace ServiceDesk.Infrastructure.Tickets;

public sealed class TicketService : ITicketService
{
    private static readonly TimeSpan SlaApproachingWindow = TimeSpan.FromHours(4);

    private readonly ApplicationDbContext _dbContext;
    private readonly ICurrentAgent _currentAgent;
    private readonly ITenantContext _tenantContext;
    private readonly TimeProvider _timeProvider;
    private Guid CurrentAgentId => _currentAgent.UserId != Guid.Empty
        ? _currentAgent.UserId
        : throw new InvalidOperationException("The current agent identity is not available.");

    public TicketService(
        ApplicationDbContext dbContext,
        ICurrentAgent currentAgent,
        ITenantContext tenantContext,
        TimeProvider timeProvider)
    {
        _dbContext = dbContext;
        _currentAgent = currentAgent;
        _tenantContext = tenantContext;
        _timeProvider = timeProvider;
    }

    public async Task<IReadOnlyList<Ticket>> GetQueueAsync(
        TicketQueueQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);
        if (query.PageNumber is < 1 or > 2_000_000)
        {
            throw new ArgumentOutOfRangeException(nameof(query.PageNumber));
        }

        if (query.PageSize is < 1 or > 100)
        {
            throw new ArgumentOutOfRangeException(nameof(query.PageSize));
        }

        var userId = CurrentAgentId;
        var memberGroupIds = _dbContext.GroupMembers
            .Where(member => member.AgentUserId == userId)
            .Select(member => member.GroupId);
        var tickets = _dbContext.Tickets
            .Include(ticket => ticket.Customer)
            .Include(ticket => ticket.Group)
            .AsQueryable();

        tickets = query.Tab switch
        {
            TicketQueueTab.MyAssignedTickets =>
                tickets.Where(ticket => ticket.AssigneeUserId == userId),
            TicketQueueTab.MyGroupsUnassigned =>
                tickets.Where(ticket => ticket.GroupId.HasValue
                    && memberGroupIds.Contains(ticket.GroupId.Value)
                    && ticket.AssigneeUserId == null),
            TicketQueueTab.AllMyGroups =>
                tickets.Where(ticket => ticket.GroupId.HasValue
                    && memberGroupIds.Contains(ticket.GroupId.Value)),
            TicketQueueTab.Triage =>
                tickets.Where(ticket => ticket.Group != null
                    && ticket.Group.IsTriageGroup
                    && ticket.GroupId.HasValue
                    && memberGroupIds.Contains(ticket.GroupId.Value)),
            TicketQueueTab.Sla =>
                tickets.Where(ticket =>
                    (ticket.AssigneeUserId == userId
                        || (ticket.GroupId.HasValue && memberGroupIds.Contains(ticket.GroupId.Value)))
                    && ticket.SlaDueAt.HasValue
                    && ticket.SlaDueAt.Value <= _timeProvider.GetUtcNow() + SlaApproachingWindow
                    && ticket.Status != TicketStatus.Resolved
                    && ticket.Status != TicketStatus.Closed),
            _ => throw new ArgumentOutOfRangeException(nameof(query.Tab), query.Tab, "Unknown queue tab.")
        };

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();
            tickets = tickets.Where(ticket =>
                ticket.Reference.Contains(search)
                || ticket.Title.Contains(search)
                || ticket.Description.Contains(search));
        }

        if (query.Status.HasValue)
        {
            tickets = tickets.Where(ticket => ticket.Status == query.Status.Value);
        }

        if (query.Type.HasValue)
        {
            tickets = tickets.Where(ticket => ticket.Type == query.Type.Value);
        }

        if (query.Priority.HasValue)
        {
            tickets = tickets.Where(ticket => ticket.Priority == query.Priority.Value);
        }

        return await tickets
            .OrderBy(ticket => ticket.TicketId)
            .Skip((query.PageNumber - 1) * query.PageSize)
            .Take(query.PageSize + 1)
            .ToListAsync(cancellationToken);
    }

    public async Task<Ticket?> GetDetailsAsync(
        Guid ticketId,
        CancellationToken cancellationToken = default)
    {
        var userId = CurrentAgentId;
        var groupIds = _dbContext.GroupMembers
            .Where(member => member.AgentUserId == userId)
            .Select(member => member.GroupId);

        return await _dbContext.Tickets
            .Where(ticket => ticket.TicketId == ticketId
                && (ticket.AssigneeUserId == userId
                    || (ticket.GroupId.HasValue && groupIds.Contains(ticket.GroupId.Value))))
            .Include(ticket => ticket.Customer)
            .Include(ticket => ticket.Group)
            .Include(ticket => ticket.Activities)
            .Include(ticket => ticket.GroupHistory)
                .ThenInclude(history => history.FromGroup)
            .Include(ticket => ticket.GroupHistory)
                .ThenInclude(history => history.ToGroup)
            .AsSplitQuery()
            .SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<TicketActivity>> GetRequesterVisibleActivitiesAsync(
        Guid ticketId,
        Guid requesterId,
        CancellationToken cancellationToken = default)
    {
        if (requesterId == Guid.Empty)
        {
            return Array.Empty<TicketActivity>();
        }

        var requesterOwnsTicket = await _dbContext.Tickets
            .AnyAsync(ticket => ticket.TicketId == ticketId
                && ticket.Customer != null
                && ticket.Customer.PrimaryContactId == requesterId,
                cancellationToken);
        if (!requesterOwnsTicket)
        {
            return Array.Empty<TicketActivity>();
        }

        var activities = await _dbContext.TicketActivities
            .Where(activity => activity.TicketId == ticketId
                && activity.Visibility == TicketActivityVisibility.Public)
            .ToListAsync(cancellationToken);
        return activities.OrderBy(activity => activity.OccurredAt).ToArray();
    }

    public async Task<IReadOnlyList<ResolverGroup>> GetActiveGroupsAsync(
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.ResolverGroups
            .Where(group => group.Active)
            .OrderBy(group => group.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<Ticket> CreateAsync(
        CreateTicketRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ValidateRequiredText(request.Reference, nameof(request.Reference), 50);
        ValidateRequiredText(request.Title, nameof(request.Title), 300);
        ValidateRequiredText(request.Description, nameof(request.Description), 20_000);
        if (request.CustomerId == Guid.Empty)
        {
            throw new ArgumentException("A customer organisation is required.", nameof(request));
        }

        var agentId = CurrentAgentId;
        var groupId = request.GroupId;
        var initialStatus = TicketStatus.New;
        if (!groupId.HasValue)
        {
            groupId = await _dbContext.ResolverGroups
                .Where(group => group.Active && group.IsTriageGroup)
                .Select(group => (Guid?)group.GroupId)
                .SingleOrDefaultAsync(cancellationToken);

            if (!groupId.HasValue)
            {
                throw new InvalidOperationException("No active triage group is configured for this tenant.");
            }

            initialStatus = TicketStatus.Triage;
        }

        var destination = await _dbContext.ResolverGroups
            .SingleOrDefaultAsync(group => group.GroupId == groupId.Value, cancellationToken);
        if (destination is null || !destination.Active)
        {
            throw new InvalidOperationException("The selected resolver group is unavailable.");
        }
        if (destination.IsTriageGroup)
        {
            initialStatus = TicketStatus.Triage;
        }

        var ticket = new Ticket
        {
            TicketId = Guid.NewGuid(),
            Reference = request.Reference.Trim(),
            Type = request.Type,
            Title = request.Title.Trim(),
            Description = request.Description.Trim(),
            Status = initialStatus,
            Priority = request.Priority,
            CustomerId = request.CustomerId,
            GroupId = destination.GroupId,
            SlaDueAt = request.SlaDueAt,
            CreatedAt = _timeProvider.GetUtcNow()
        };

        var now = ticket.CreatedAt;
        ticket.GroupHistory.Add(new TicketGroupHistory
        {
            HistoryId = Guid.NewGuid(),
            TenantId = _tenantContext.TenantId,
            TicketId = ticket.TicketId,
            FromGroupId = null,
            ToGroupId = destination.GroupId,
            ChangedBy = agentId,
            ChangedAt = now
        });

        _dbContext.Tickets.Add(ticket);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return ticket;
    }

    public async Task TransferAsync(
        Guid ticketId,
        Guid destinationGroupId,
        CancellationToken cancellationToken = default)
    {
        if (destinationGroupId == Guid.Empty)
        {
            throw new ArgumentException("A destination group is required.", nameof(destinationGroupId));
        }

        var ticket = await GetAccessibleTicketForUpdateAsync(ticketId, cancellationToken);
        var destination = await _dbContext.ResolverGroups
            .SingleOrDefaultAsync(group => group.GroupId == destinationGroupId, cancellationToken);
        if (destination is null || !destination.Active)
        {
            throw new InvalidOperationException("The destination resolver group is unavailable.");
        }

        if (ticket.GroupId == destination.GroupId)
        {
            return;
        }

        var now = _timeProvider.GetUtcNow();
        _dbContext.TicketGroupHistories.Add(new TicketGroupHistory
        {
            HistoryId = Guid.NewGuid(),
            TenantId = ticket.TenantId,
            TicketId = ticket.TicketId,
            FromGroupId = ticket.GroupId,
            ToGroupId = destination.GroupId,
            ChangedBy = CurrentAgentId,
            ChangedAt = now
        });
        ticket.GroupId = destination.GroupId;
        ticket.Group = destination;
        ticket.AssigneeUserId = null;

        _dbContext.TicketActivities.Add(new TicketActivity
        {
            ActivityId = Guid.NewGuid(),
            TenantId = ticket.TenantId,
            TicketId = ticket.TicketId,
            ActorId = CurrentAgentId,
            OccurredAt = now,
            Kind = TicketActivityKind.System,
            Visibility = TicketActivityVisibility.Internal,
            Content = $"Ticket transferred to {destination.Name}."
        });

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public Task AddPublicReplyAsync(
        Guid ticketId,
        string content,
        CancellationToken cancellationToken = default) =>
        AddActivityAsync(
            ticketId,
            content,
            TicketActivityKind.PublicReply,
            TicketActivityVisibility.Public,
            cancellationToken);

    public Task AddInternalNoteAsync(
        Guid ticketId,
        string content,
        CancellationToken cancellationToken = default) =>
        AddActivityAsync(
            ticketId,
            content,
            TicketActivityKind.InternalNote,
            TicketActivityVisibility.Internal,
            cancellationToken);

    private async Task AddActivityAsync(
        Guid ticketId,
        string content,
        TicketActivityKind kind,
        TicketActivityVisibility visibility,
        CancellationToken cancellationToken)
    {
        ValidateRequiredText(content, nameof(content), 20_000);
        var ticket = await GetAccessibleTicketForUpdateAsync(ticketId, cancellationToken);
        if (ticket.Status is TicketStatus.Resolved or TicketStatus.Closed)
        {
            throw new InvalidOperationException("Replies and notes cannot be added to a resolved or closed ticket.");
        }

        _dbContext.TicketActivities.Add(new TicketActivity
        {
            ActivityId = Guid.NewGuid(),
            TenantId = ticket.TenantId,
            TicketId = ticket.TicketId,
            ActorId = CurrentAgentId,
            OccurredAt = _timeProvider.GetUtcNow(),
            Kind = kind,
            Visibility = visibility,
            Content = content.Trim()
        });
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task<Ticket> GetAccessibleTicketForUpdateAsync(
        Guid ticketId,
        CancellationToken cancellationToken)
    {
        var agentId = CurrentAgentId;
        var groupIds = _dbContext.GroupMembers
            .Where(member => member.AgentUserId == agentId)
            .Select(member => member.GroupId);

        var ticket = await _dbContext.Tickets
            .Include(item => item.GroupHistory)
            .SingleOrDefaultAsync(item => item.TicketId == ticketId
                && (item.AssigneeUserId == agentId
                    || (item.GroupId.HasValue && groupIds.Contains(item.GroupId.Value))),
                cancellationToken);

        return ticket ?? throw new KeyNotFoundException("Ticket was not found or is not available to this agent.");
    }

    private static void ValidateRequiredText(string value, string parameterName, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Trim().Length > maxLength)
        {
            throw new ArgumentException(
                $"A value containing 1 to {maxLength} characters is required.",
                parameterName);
        }
    }
}
