using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using ServiceDesk.Domain.Abstractions;
using ServiceDesk.Domain.Entities;
using ServiceDesk.Domain.Enums;
using ServiceDesk.Infrastructure.Data;

namespace ServiceDesk.Infrastructure.Sla;

public sealed class TicketSlaService : ITicketSlaService
{
    private static readonly JsonSerializerOptions JsonOptions = CreateJsonOptions();
    private readonly ApplicationDbContext _dbContext;
    private readonly ICurrentAgent _currentAgent;
    private readonly TimeProvider _timeProvider;

    public TicketSlaService(
        ApplicationDbContext dbContext,
        ICurrentAgent currentAgent,
        TimeProvider timeProvider)
    {
        _dbContext = dbContext;
        _currentAgent = currentAgent;
        _timeProvider = timeProvider;
    }

    public async Task<TicketSlaEvaluation> EvaluateAsync(
        Guid ticketId,
        CancellationToken cancellationToken = default)
    {
        var ticket = await LoadTicketAsync(ticketId, cancellationToken);
        var now = _timeProvider.GetUtcNow();
        var policy = await GetPolicyAsync(ticket, cancellationToken);
        if (policy is null && ticket.SlaPolicyId is null)
        {
            return await BuildUnconfiguredEvaluationAsync(ticket, now, cancellationToken);
        }

        if (ticket.SlaPolicyId is null)
        {
            SetPolicySnapshot(ticket, policy!);
        }

        if (ticket.ResponseDueAt is null || ticket.ResolutionDueAt is null)
        {
            if (policy is null)
            {
                throw new InvalidOperationException(
                    "The ticket's assigned SLA policy no longer exists and its deadlines were not initialized.");
            }

            ticket.ResponseDueAt = ticket.CreatedAt.AddMinutes(policy.ResponseMinutes)
                .AddTicks(ticket.SlaPausedTotalTicks);
            ticket.ResolutionDueAt = ticket.CreatedAt.AddMinutes(policy.ResolutionMinutes)
                .AddTicks(ticket.SlaPausedTotalTicks);
        }

        var pauseStatuses = DeserializePauseStatuses(ticket.SlaPauseStatusesJson);
        var statusEnteredAt = await GetCurrentStatusEnteredAtAsync(ticket, cancellationToken);
        SynchronizePauseState(ticket, pauseStatuses, now, statusEnteredAt);
        var responseDueAt = ticket.ResponseDueAt.Value;
        var resolutionDueAt = ticket.ResolutionDueAt.Value;
        if (ticket.SlaPausedAt is DateTimeOffset pausedAt)
        {
            var pausedElapsed = now - pausedAt;
            responseDueAt = responseDueAt.Add(pausedElapsed);
            resolutionDueAt = resolutionDueAt.Add(pausedElapsed);
        }

        if (ticket.FirstResponseAt is null)
        {
            var publicResponseTimes = await _dbContext.TicketActivities
                .Where(activity => activity.TicketId == ticket.TicketId
                    && activity.Kind == TicketActivityKind.PublicReply
                    && activity.Visibility == TicketActivityVisibility.Public)
                .Select(activity => (DateTimeOffset?)activity.OccurredAt)
                .ToListAsync(cancellationToken);
            ticket.FirstResponseAt = publicResponseTimes.Min();
        }

        var responseIsBreached = ticket.ResponseBreachedAt is not null
            || (ticket.FirstResponseAt is DateTimeOffset responseAt
                ? responseAt > responseDueAt
                : now > responseDueAt && ticket.SlaPausedAt is null);
        if (responseIsBreached && ticket.ResponseBreachedAt is null)
        {
            ticket.ResponseBreachedAt = responseDueAt;
        }

        var resolutionIsBreached = ticket.ResolutionBreachedAt is not null
            || (ticket.ResolvedAt is DateTimeOffset resolvedAt
                ? resolvedAt > resolutionDueAt
                : ticket.Status != TicketStatus.Closed
                    && now > resolutionDueAt
                    && ticket.SlaPausedAt is null);
        if (resolutionIsBreached && ticket.ResolutionBreachedAt is null)
        {
            ticket.ResolutionBreachedAt = resolutionDueAt;
        }

        var publicUpdateTimes = await _dbContext.TicketActivities
            .Where(activity => activity.TicketId == ticket.TicketId
                && activity.Visibility == TicketActivityVisibility.Public)
            .Select(activity => (DateTimeOffset?)activity.OccurredAt)
            .ToListAsync(cancellationToken);
        var lastPublicUpdate = publicUpdateTimes.Max() ?? ticket.CreatedAt;

        await _dbContext.SaveChangesAsync(cancellationToken);

        var ticketAge = NonNegative(now - ticket.CreatedAt);
        return new TicketSlaEvaluation(
            ticket.TicketId,
            true,
            null,
            ticket.SlaPolicyId,
            responseDueAt,
            resolutionDueAt,
            ticket.FirstResponseAt,
            ticket.SlaPausedAt is not null,
            responseIsBreached,
            resolutionIsBreached,
            ticket.ResponseBreachedAt,
            ticket.ResolutionBreachedAt,
            NonNegative(now - statusEnteredAt),
            NonNegative(now - lastPublicUpdate),
            ticketAge,
            GetAgeBand(ticketAge));
    }

    public async Task<TicketSlaEvaluation> TransitionStatusAsync(
        Guid ticketId,
        TicketStatus newStatus,
        CancellationToken cancellationToken = default)
    {
        if (!Enum.IsDefined(newStatus))
        {
            throw new ArgumentOutOfRangeException(nameof(newStatus));
        }

        var ticket = await LoadTicketAsync(ticketId, cancellationToken);
        var now = _timeProvider.GetUtcNow();
        if (ticket.Status == newStatus)
        {
            return await EvaluateAsync(ticketId, cancellationToken);
        }

        var agentId = _currentAgent.UserId;
        if (agentId == Guid.Empty)
        {
            throw new InvalidOperationException("The current agent identity is not available.");
        }

        var policy = await GetPolicyAsync(ticket, cancellationToken);
        var pauseStatuses = ticket.SlaPolicyId is null
            ? policy is null
                ? Array.Empty<TicketStatus>()
                : SetPolicySnapshot(ticket, policy)
            : DeserializePauseStatuses(ticket.SlaPauseStatusesJson);

        if (ticket.SlaPolicyId is null && policy is null)
        {
            if (newStatus is TicketStatus.Resolved or TicketStatus.Closed)
            {
                ticket.ResolvedAt ??= now;
            }
            else if (ticket.ResolvedAt.HasValue)
            {
                ticket.ResolvedAt = null;
            }

            ticket.StatusHistory.Add(new TicketStatusHistory
            {
                StatusHistoryId = Guid.NewGuid(),
                TenantId = ticket.TenantId,
                TicketId = ticket.TicketId,
                Status = newStatus,
                EnteredAt = now,
                ChangedBy = agentId
            });
            ticket.Status = newStatus;
            await _dbContext.SaveChangesAsync(cancellationToken);
            return await BuildUnconfiguredEvaluationAsync(ticket, now, cancellationToken);
        }

        if (ticket.ResponseDueAt is null || ticket.ResolutionDueAt is null)
        {
            if (policy is null)
            {
                throw new InvalidOperationException(
                    "The ticket's assigned SLA policy no longer exists and its deadlines were not initialized.");
            }

            ticket.ResponseDueAt = ticket.CreatedAt.AddMinutes(policy.ResponseMinutes)
                .AddTicks(ticket.SlaPausedTotalTicks);
            ticket.ResolutionDueAt = ticket.CreatedAt.AddMinutes(policy.ResolutionMinutes)
                .AddTicks(ticket.SlaPausedTotalTicks);
        }

        var wasPaused = pauseStatuses.Contains(ticket.Status);
        var willBePaused = pauseStatuses.Contains(newStatus);
        if (wasPaused && ticket.SlaPausedAt is null)
        {
            ticket.SlaPausedAt = await GetCurrentStatusEnteredAtAsync(ticket, cancellationToken);
        }

        if (ticket.FirstResponseAt is null)
        {
            var publicResponseTimes = await _dbContext.TicketActivities
                .Where(activity => activity.TicketId == ticket.TicketId
                    && activity.Kind == TicketActivityKind.PublicReply
                    && activity.Visibility == TicketActivityVisibility.Public)
                .Select(activity => (DateTimeOffset?)activity.OccurredAt)
                .ToListAsync(cancellationToken);
            ticket.FirstResponseAt = publicResponseTimes.Min();
        }

        var activePauseDuration = ticket.SlaPausedAt is DateTimeOffset pauseStartedAt
            ? now - pauseStartedAt
            : TimeSpan.Zero;
        var effectiveResponseDueAt = ticket.ResponseDueAt.Value.Add(activePauseDuration);
        var effectiveResolutionDueAt = ticket.ResolutionDueAt.Value.Add(activePauseDuration);

        if (ticket.ResponseBreachedAt is null
            && (ticket.FirstResponseAt is DateTimeOffset responseAt
                ? responseAt > effectiveResponseDueAt
                : now > effectiveResponseDueAt))
        {
            ticket.ResponseBreachedAt = effectiveResponseDueAt;
        }

        if (ticket.ResolutionBreachedAt is null
            && (ticket.ResolvedAt is DateTimeOffset resolvedAt
                ? resolvedAt > effectiveResolutionDueAt
                : now > effectiveResolutionDueAt))
        {
            ticket.ResolutionBreachedAt = effectiveResolutionDueAt;
        }

        if (!wasPaused && willBePaused)
        {
            ticket.SlaPausedAt = now;
        }
        else if (wasPaused && !willBePaused && ticket.SlaPausedAt is DateTimeOffset pausedAt)
        {
            var pauseDuration = now - pausedAt;
            ticket.ResponseDueAt = ticket.ResponseDueAt.Value.Add(pauseDuration);
            ticket.ResolutionDueAt = ticket.ResolutionDueAt.Value.Add(pauseDuration);
            ticket.SlaPausedTotalTicks = checked(ticket.SlaPausedTotalTicks + pauseDuration.Ticks);
            ticket.SlaPausedAt = null;
        }

        ticket.Status = newStatus;
        if (newStatus is TicketStatus.Resolved or TicketStatus.Closed)
        {
            ticket.ResolvedAt ??= now;
        }
        else if (ticket.ResolvedAt.HasValue)
        {
            ticket.ResolvedAt = null;
        }

        ticket.StatusHistory.Add(new TicketStatusHistory
        {
            StatusHistoryId = Guid.NewGuid(),
            TenantId = ticket.TenantId,
            TicketId = ticket.TicketId,
            Status = newStatus,
            EnteredAt = now,
            ChangedBy = agentId
        });

        await _dbContext.SaveChangesAsync(cancellationToken);
        return await EvaluateAsync(ticketId, cancellationToken);
    }

    private async Task<Ticket> LoadTicketAsync(Guid ticketId, CancellationToken cancellationToken)
    {
        if (ticketId == Guid.Empty)
        {
            throw new ArgumentException("A ticket ID is required.", nameof(ticketId));
        }

        return await _dbContext.Tickets
            .SingleOrDefaultAsync(ticket => ticket.TicketId == ticketId, cancellationToken)
            ?? throw new KeyNotFoundException("Ticket was not found in the current tenant.");
    }

    private Task<SlaPolicy?> GetPolicyAsync(Ticket ticket, CancellationToken cancellationToken)
    {
        if (ticket.SlaPolicyId is Guid policyId)
        {
            return _dbContext.SlaPolicies
                .SingleOrDefaultAsync(policy => policy.PolicyId == policyId, cancellationToken);
        }

        return _dbContext.SlaPolicies.SingleOrDefaultAsync(policy =>
            policy.CustomerId == ticket.CustomerId
            && policy.ServiceId == ticket.CatalogueItemId
            && policy.Type == ticket.Type
            && policy.Priority == ticket.Priority,
            cancellationToken);
    }

    private static TicketStatus[] SetPolicySnapshot(Ticket ticket, SlaPolicy policy)
    {
        if (policy.ResponseMinutes is < 1 or > 525600
            || policy.ResolutionMinutes is < 1 or > 525600)
        {
            throw new InvalidOperationException("The matching SLA policy must have positive response and resolution times.");
        }

        var pauseStatuses = DeserializePauseStatuses(policy.PausedStatusesJson);
        ticket.SlaPolicyId = policy.PolicyId;
        ticket.SlaPauseStatusesJson = JsonSerializer.Serialize(pauseStatuses, JsonOptions);
        return pauseStatuses;
    }

    private static TicketStatus[] DeserializePauseStatuses(string json)
    {
        try
        {
            var statuses = JsonSerializer.Deserialize<TicketStatus[]>(json, JsonOptions);
            if (statuses is null || statuses.Any(status => !Enum.IsDefined(status)))
            {
                throw new InvalidOperationException("SLA policy pause statuses are invalid.");
            }

            return statuses.Distinct().ToArray();
        }
        catch (JsonException exception)
        {
            throw new InvalidOperationException("SLA policy pause statuses are invalid JSON.", exception);
        }
    }

    private static void SynchronizePauseState(
        Ticket ticket,
        IReadOnlyCollection<TicketStatus> pauseStatuses,
        DateTimeOffset now,
        DateTimeOffset statusEnteredAt)
    {
        if (pauseStatuses.Contains(ticket.Status))
        {
            ticket.SlaPausedAt ??= statusEnteredAt;
        }
        else if (ticket.SlaPausedAt is DateTimeOffset pausedAt)
        {
            var pauseDuration = now - pausedAt;
            ticket.ResponseDueAt = ticket.ResponseDueAt?.Add(pauseDuration);
            ticket.ResolutionDueAt = ticket.ResolutionDueAt?.Add(pauseDuration);
            ticket.SlaPausedTotalTicks = checked(ticket.SlaPausedTotalTicks + pauseDuration.Ticks);
            ticket.SlaPausedAt = null;
        }
    }

    private async Task<DateTimeOffset> GetCurrentStatusEnteredAtAsync(
        Ticket ticket,
        CancellationToken cancellationToken)
    {
        var statusEntryTimes = await _dbContext.TicketStatusHistories
            .Where(history => history.TicketId == ticket.TicketId && history.Status == ticket.Status)
            .Select(history => (DateTimeOffset?)history.EnteredAt)
            .ToListAsync(cancellationToken);
        return statusEntryTimes.Max() ?? ticket.CreatedAt;
    }

    private async Task<TicketSlaEvaluation> BuildUnconfiguredEvaluationAsync(
        Ticket ticket,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var statusEnteredAt = await GetCurrentStatusEnteredAtAsync(ticket, cancellationToken);
        var publicUpdateTimes = await _dbContext.TicketActivities
            .Where(activity => activity.TicketId == ticket.TicketId
                && activity.Visibility == TicketActivityVisibility.Public)
            .Select(activity => (DateTimeOffset?)activity.OccurredAt)
            .ToListAsync(cancellationToken);
        var lastPublicUpdate = publicUpdateTimes.Max() ?? ticket.CreatedAt;

        return new TicketSlaEvaluation(
            ticket.TicketId,
            false,
            "No SLA policy matches this ticket's customer, service, type, and priority.",
            null,
            null,
            null,
            null,
            false,
            false,
            false,
            null,
            null,
            NonNegative(now - statusEnteredAt),
            NonNegative(now - lastPublicUpdate),
            NonNegative(now - ticket.CreatedAt),
            GetAgeBand(NonNegative(now - ticket.CreatedAt)));
    }

    private static TimeSpan NonNegative(TimeSpan value) =>
        value < TimeSpan.Zero ? TimeSpan.Zero : value;

    private static JsonSerializerOptions CreateJsonOptions()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        options.Converters.Add(new JsonStringEnumConverter());
        return options;
    }

    private static ElapsedAgeBand GetAgeBand(TimeSpan age) =>
        age.TotalDays switch
        {
            < 2 => ElapsedAgeBand.Day0To1,
            < 4 => ElapsedAgeBand.Day2To3,
            < 8 => ElapsedAgeBand.Day4To7,
            _ => ElapsedAgeBand.Day8Plus
        };
}
