using ServiceDesk.Domain.Entities;
using ServiceDesk.Domain.Enums;

namespace ServiceDesk.Infrastructure.Portal;

public interface IPortalService
{
    Task<IReadOnlyList<CatalogueItem>> GetPublishedCatalogueAsync(
        string? search,
        CancellationToken cancellationToken = default);

    Task<CatalogueItem?> GetPublishedCatalogueItemAsync(
        Guid itemId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<KnowledgeArticle>> SearchKnowledgeAsync(
        string? search,
        CancellationToken cancellationToken = default);

    Task<Ticket> SubmitIncidentAsync(
        string title,
        string description,
        TicketPriority priority,
        CancellationToken cancellationToken = default);

    Task<Ticket> SubmitServiceRequestAsync(
        Guid itemId,
        IReadOnlyDictionary<string, string?> answers,
        bool eligibilityConfirmed,
        CancellationToken cancellationToken = default);
}
