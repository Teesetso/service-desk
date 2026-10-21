using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ServiceDesk.Domain.Entities;
using ServiceDesk.Domain.Enums;
using ServiceDesk.Infrastructure.Tickets;

namespace ServiceDesk.Web.Pages.Tickets;

[Authorize(Policy = "Agents")]
public sealed class IndexModel : PageModel
{
    private readonly ITicketService _ticketService;

    public IndexModel(ITicketService ticketService)
    {
        _ticketService = ticketService;
    }

    [BindProperty(SupportsGet = true)]
    public TicketQueueTab Tab { get; set; } = TicketQueueTab.MyAssignedTickets;

    [BindProperty(SupportsGet = true)]
    public int PageNumber { get; set; } = 1;

    [BindProperty(SupportsGet = true)]
    public string? Search { get; set; }

    [BindProperty(SupportsGet = true)]
    public TicketStatus? Status { get; set; }

    [BindProperty(SupportsGet = true)]
    public TicketType? Type { get; set; }

    [BindProperty(SupportsGet = true)]
    public TicketPriority? Priority { get; set; }

    public IReadOnlyList<Ticket> Tickets { get; private set; } = Array.Empty<Ticket>();

    public bool HasNextPage { get; private set; }

    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
    {
        if (!Enum.IsDefined(Tab) || PageNumber is < 1 or > 2_000_000)
        {
            return BadRequest("Invalid ticket queue or page.");
        }

        var items = await _ticketService.GetQueueAsync(new TicketQueueQuery
        {
            Tab = Tab,
            PageNumber = PageNumber,
            Search = Search,
            Status = Status,
            Type = Type,
            Priority = Priority
        }, cancellationToken);
        HasNextPage = items.Count > 50;
        Tickets = HasNextPage ? items.Take(50).ToArray() : items;

        return Page();
    }
}
