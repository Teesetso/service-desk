using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using ServiceDesk.Infrastructure.Portal;
using ServiceDesk.Infrastructure.Sla;

namespace ServiceDesk.Infrastructure.Tickets;

public static class TicketServiceCollectionExtensions
{
    public static IServiceCollection AddTicketOperations(this IServiceCollection services)
    {
        services.TryAddSingleton(TimeProvider.System);
        services.AddScoped<ITicketService, TicketService>();
        services.AddScoped<IPortalService, PortalService>();
        services.AddScoped<ITicketSlaService, TicketSlaService>();
        return services;
    }
}
