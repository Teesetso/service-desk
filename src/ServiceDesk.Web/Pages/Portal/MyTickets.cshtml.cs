using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using ServiceDesk.Domain.Entities;
using ServiceDesk.Infrastructure.Data;
using ServiceDesk.Web.Infrastructure;

namespace ServiceDesk.Web.Pages.Portal;

[Authorize(Policy = "Customers")]
public sealed class MyTicketsModel : PageModel
{
    private readonly ApplicationDbContext _dbContext;
    private readonly DevelopmentRequestContext _requestContext;

    public MyTicketsModel(
        ApplicationDbContext dbContext,
        DevelopmentRequestContext requestContext)
    {
        _dbContext = dbContext;
        _requestContext = requestContext;
    }

    public IReadOnlyList<Ticket> Tickets { get; private set; } = Array.Empty<Ticket>();

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        var tickets = await _dbContext.Tickets
            .Include(ticket => ticket.Group)
            .Where(ticket => ticket.CustomerId == _requestContext.CustomerId)
            .ToListAsync(cancellationToken);
        Tickets = tickets
            .OrderByDescending(ticket => ticket.CreatedAt)
            .ToArray();
    }
}
