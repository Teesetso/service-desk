using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ServiceDesk.Domain.Entities;
using ServiceDesk.Domain.Enums;
using ServiceDesk.Infrastructure.Portal;

namespace ServiceDesk.Web.Pages.Portal;

[Authorize]
public sealed class IndexModel : PageModel
{
    private readonly IPortalService _portalService;

    public IndexModel(IPortalService portalService)
    {
        _portalService = portalService;
    }

    [BindProperty(SupportsGet = true)]
    public string? CatalogueSearch { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? KnowledgeSearch { get; set; }

    [BindProperty(SupportsGet = true)]
    public Guid? SelectedItemId { get; set; }

    [BindProperty]
    public Guid ItemId { get; set; }

    [BindProperty]
    public Dictionary<string, string?> Answers { get; set; } = new(StringComparer.Ordinal);

    [BindProperty]
    public bool EligibilityConfirmed { get; set; }

    [BindProperty]
    public string IncidentTitle { get; set; } = string.Empty;

    [BindProperty]
    public string IncidentDescription { get; set; } = string.Empty;

    [BindProperty]
    public TicketPriority IncidentPriority { get; set; } = TicketPriority.Normal;

    public IReadOnlyList<CatalogueItem> CatalogueItems { get; private set; } =
        Array.Empty<CatalogueItem>();

    public IReadOnlyList<KnowledgeArticle> KnowledgeArticles { get; private set; } =
        Array.Empty<KnowledgeArticle>();

    public CatalogueItem? SelectedItem { get; private set; }

    public IReadOnlyList<CatalogueFormField> FormFields { get; private set; } =
        Array.Empty<CatalogueFormField>();

    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
    {
        if ((CatalogueSearch?.Length ?? 0) > 200 || (KnowledgeSearch?.Length ?? 0) > 200)
        {
            return BadRequest("Search terms must be 200 characters or fewer.");
        }

        await LoadPortalAsync(cancellationToken);
        return Page();
    }

    public async Task<IActionResult> OnPostSubmitIncidentAsync(CancellationToken cancellationToken)
    {
        try
        {
            var ticket = await _portalService.SubmitIncidentAsync(
                IncidentTitle, IncidentDescription, IncidentPriority, cancellationToken);
            TempData["PortalStatusMessage"] = $"Incident submitted. Reference: {ticket.Reference}";
            return RedirectToPage();
        }
        catch (ArgumentException exception)
        {
            ModelState.AddModelError(string.Empty, exception.Message);
        }

        await LoadPortalAsync(cancellationToken);
        return Page();
    }

    public async Task<IActionResult> OnPostSubmitRequestAsync(CancellationToken cancellationToken)
    {
        try
        {
            var ticket = await _portalService.SubmitServiceRequestAsync(
                ItemId, Answers, EligibilityConfirmed, cancellationToken);
            TempData["PortalStatusMessage"] = $"Request submitted. Reference: {ticket.Reference}";
            return RedirectToPage();
        }
        catch (ArgumentException exception)
        {
            ModelState.AddModelError(string.Empty, exception.Message);
        }
        catch (CatalogueItemUnavailableException exception)
        {
            ModelState.AddModelError(string.Empty, exception.Message);
        }

        SelectedItemId = ItemId;
        await LoadPortalAsync(cancellationToken);
        return Page();
    }

    private async Task LoadPortalAsync(CancellationToken cancellationToken)
    {
        CatalogueItems = await _portalService.GetPublishedCatalogueAsync(
            CatalogueSearch, cancellationToken);
        KnowledgeArticles = await _portalService.SearchKnowledgeAsync(
            KnowledgeSearch, cancellationToken);

        if (SelectedItemId is not Guid selectedItemId || selectedItemId == Guid.Empty)
        {
            return;
        }

        SelectedItem = await _portalService.GetPublishedCatalogueItemAsync(
            selectedItemId, cancellationToken);
        if (SelectedItem is null)
        {
            ModelState.AddModelError(
                string.Empty, "This catalogue item is no longer available for new submissions.");
            return;
        }

        try
        {
            FormFields = JsonSerializer.Deserialize<IReadOnlyList<CatalogueFormField>>(
                SelectedItem.FormSchemaJson,
                new JsonSerializerOptions(JsonSerializerDefaults.Web))
                ?? throw new InvalidOperationException("The catalogue form is not configured correctly.");
        }
        catch (JsonException exception)
        {
            throw new InvalidOperationException("The catalogue form is not configured correctly.", exception);
        }
    }
}
