using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ServiceDesk.Domain.Entities;
using ServiceDesk.Domain.Enums;
using ServiceDesk.Domain.Abstractions;
using ServiceDesk.Infrastructure.Tickets;

namespace ServiceDesk.Web.Pages.Tickets;

[Authorize(Policy = "Agents")]
public sealed class DetailsModel : PageModel
{
    private readonly ITicketService _ticketService;
    private readonly ICurrentAgent _currentAgent;

    public DetailsModel(ITicketService ticketService, ICurrentAgent currentAgent)
    {
        _ticketService = ticketService;
        _currentAgent = currentAgent;
    }

    public Ticket Ticket { get; private set; } = null!;

    public IReadOnlyList<ResolverGroup> ActiveGroups { get; private set; } =
        Array.Empty<ResolverGroup>();

    [BindProperty]
    public string? ActivityContent { get; set; }

    [BindProperty]
    public Guid DestinationGroupId { get; set; }

    [BindProperty]
    public TicketStatus NewStatus { get; set; }

    public bool IsAssignedToCurrentAgent =>
        Ticket.AssigneeUserId == _currentAgent.UserId;

    public async Task<IActionResult> OnGetAsync(Guid id, CancellationToken cancellationToken)
    {
        return await LoadPageAsync(id, cancellationToken);
    }

    public Task<IActionResult> OnPostPublicReplyAsync(Guid id, CancellationToken cancellationToken) =>
        AddActivityAsync(id, isPublicReply: true, cancellationToken);

    public Task<IActionResult> OnPostInternalNoteAsync(Guid id, CancellationToken cancellationToken) =>
        AddActivityAsync(id, isPublicReply: false, cancellationToken);

    public async Task<IActionResult> OnPostAssignToMeAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        try
        {
            await _ticketService.AssignToCurrentAgentAsync(id, cancellationToken);
            TempData["StatusMessage"] = "Ticket assigned to you.";
            return RedirectToPage(new { id });
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    public async Task<IActionResult> OnPostUpdateStatusAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        try
        {
            await _ticketService.UpdateStatusAsync(id, NewStatus, cancellationToken);
            TempData["StatusMessage"] = "Ticket status updated.";
            return RedirectToPage(new { id });
        }
        catch (ArgumentException exception)
        {
            ModelState.AddModelError(nameof(NewStatus), exception.Message);
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }

        return await LoadPageAsync(id, cancellationToken);
    }

    public async Task<IActionResult> OnPostTransferAsync(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            await _ticketService.TransferAsync(id, DestinationGroupId, cancellationToken);
            TempData["StatusMessage"] = "Ticket transferred.";
            return RedirectToPage(new { id });
        }
        catch (ArgumentException exception)
        {
            ModelState.AddModelError(nameof(DestinationGroupId), exception.Message);
        }
        catch (InvalidOperationException exception)
        {
            ModelState.AddModelError(nameof(DestinationGroupId), exception.Message);
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }

        return await LoadPageAsync(id, cancellationToken);
    }

    private async Task<IActionResult> AddActivityAsync(
        Guid id,
        bool isPublicReply,
        CancellationToken cancellationToken)
    {
        try
        {
            if (isPublicReply)
            {
                await _ticketService.AddPublicReplyAsync(id, ActivityContent ?? string.Empty, cancellationToken);
                TempData["StatusMessage"] = "Public reply added.";
            }
            else
            {
                await _ticketService.AddInternalNoteAsync(id, ActivityContent ?? string.Empty, cancellationToken);
                TempData["StatusMessage"] = "Internal note added.";
            }

            return RedirectToPage(new { id });
        }
        catch (ArgumentException exception)
        {
            ModelState.AddModelError(nameof(ActivityContent), exception.Message);
        }
        catch (InvalidOperationException exception)
        {
            ModelState.AddModelError(nameof(ActivityContent), exception.Message);
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }

        return await LoadPageAsync(id, cancellationToken);
    }

    private async Task<IActionResult> LoadPageAsync(Guid id, CancellationToken cancellationToken)
    {
        var ticket = await _ticketService.GetDetailsAsync(id, cancellationToken);
        if (ticket is null)
        {
            return NotFound();
        }

        Ticket = ticket;
        ActiveGroups = await _ticketService.GetActiveGroupsAsync(cancellationToken);
        return Page();
    }
}
