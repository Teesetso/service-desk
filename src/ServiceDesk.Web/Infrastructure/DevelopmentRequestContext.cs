using ServiceDesk.Domain.Abstractions;

namespace ServiceDesk.Web.Infrastructure;

public sealed class DevelopmentRequestContext : ITenantContext, ICurrentAgent, ICurrentRequester
{
    public static readonly Guid TenantIdValue = Guid.Parse("10d56e8d-2d35-4f2a-aac0-e6bfad1c2663");
    public static readonly Guid UserIdValue = Guid.Parse("20d56e8d-2d35-4f2a-aac0-e6bfad1c2663");
    public static readonly Guid CustomerIdValue = Guid.Parse("30d56e8d-2d35-4f2a-aac0-e6bfad1c2663");

    public Guid TenantId => TenantIdValue;
    public Guid UserId => UserIdValue;
    public Guid CustomerId => CustomerIdValue;
}
