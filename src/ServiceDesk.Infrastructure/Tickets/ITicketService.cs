using ServiceDesk.Domain.Entities;

namespace ServiceDesk.Infrastructure.Tickets;

public interface ITicketService
{
    Task<IReadOnlyList<Ticket>> GetQueueAsync(
        TicketQueueQuery query,
        CancellationToken cancellationToken = default);

    Task<Ticket?> GetDetailsAsync(Guid ticketId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TicketActivity>> GetRequesterVisibleActivitiesAsync(
        Guid ticketId,
        Guid requesterId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ResolverGroup>> GetActiveGroupsAsync(
        CancellationToken cancellationToken = default);

    Task<Ticket> CreateAsync(
        CreateTicketRequest request,
        CancellationToken cancellationToken = default);

    Task TransferAsync(
        Guid ticketId,
        Guid destinationGroupId,
        CancellationToken cancellationToken = default);

    Task AddPublicReplyAsync(
        Guid ticketId,
        string content,
        CancellationToken cancellationToken = default);

    Task AddInternalNoteAsync(
        Guid ticketId,
        string content,
        CancellationToken cancellationToken = default);
}

public enum TicketQueueTab
{
    MyAssignedTickets,
    MyGroupsUnassigned,
    AllMyGroups,
    Triage,
    Sla
}

public sealed class TicketQueueQuery
{
    public TicketQueueTab Tab { get; init; } = TicketQueueTab.MyAssignedTickets;
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 50;
    public string? Search { get; init; }
    public ServiceDesk.Domain.Enums.TicketStatus? Status { get; init; }
    public ServiceDesk.Domain.Enums.TicketType? Type { get; init; }
    public ServiceDesk.Domain.Enums.TicketPriority? Priority { get; init; }
}

public sealed class CreateTicketRequest
{
    public string Reference { get; init; } = string.Empty;
    public ServiceDesk.Domain.Enums.TicketType Type { get; init; }
    public string Title { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public ServiceDesk.Domain.Enums.TicketPriority Priority { get; init; } =
        ServiceDesk.Domain.Enums.TicketPriority.Normal;
    public Guid CustomerId { get; init; }
    public Guid? GroupId { get; init; }
    public DateTimeOffset? SlaDueAt { get; init; }
}
