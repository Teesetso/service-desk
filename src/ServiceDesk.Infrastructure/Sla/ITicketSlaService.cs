using ServiceDesk.Domain.Entities;
using ServiceDesk.Domain.Enums;

namespace ServiceDesk.Infrastructure.Sla;

public interface ITicketSlaService
{
    Task<TicketSlaEvaluation> EvaluateAsync(
        Guid ticketId,
        CancellationToken cancellationToken = default);

    Task<TicketSlaEvaluation> TransitionStatusAsync(
        Guid ticketId,
        TicketStatus newStatus,
        CancellationToken cancellationToken = default);
}

public sealed record TicketSlaEvaluation(
    Guid TicketId,
    bool IsConfigured,
    string? ConfigurationMessage,
    Guid? PolicyId,
    DateTimeOffset? ResponseDueAt,
    DateTimeOffset? ResolutionDueAt,
    DateTimeOffset? FirstResponseAt,
    bool IsSlaPaused,
    bool IsResponseBreached,
    bool IsResolutionBreached,
    DateTimeOffset? ResponseBreachedAt,
    DateTimeOffset? ResolutionBreachedAt,
    TimeSpan ElapsedInCurrentStatus,
    TimeSpan TimeSinceLastPublicUpdate,
    TimeSpan ElapsedAge,
    ElapsedAgeBand AgeBand);
