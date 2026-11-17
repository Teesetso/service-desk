using System.Security.Claims;
using ServiceDesk.Domain.Abstractions;

namespace ServiceDesk.Web.Infrastructure;

public sealed class DevelopmentRequestContext : ITenantContext, ICurrentAgent, ICurrentRequester
{
    public static readonly Guid TenantIdValue = Guid.Parse("10d56e8d-2d35-4f2a-aac0-e6bfad1c2663");
    public static readonly Guid AgentUserIdValue = Guid.Parse("20d56e8d-2d35-4f2a-aac0-e6bfad1c2663");
    public static readonly Guid AdminUserIdValue = Guid.Parse("20d56e8d-2d35-4f2a-aac0-e6bfad1c2665");
    public static readonly Guid ClientUserIdValue = Guid.Parse("20d56e8d-2d35-4f2a-aac0-e6bfad1c2666");
    public static readonly Guid UserIdValue = AgentUserIdValue;
    public static readonly Guid CustomerIdValue = Guid.Parse("30d56e8d-2d35-4f2a-aac0-e6bfad1c2663");

    private readonly IHttpContextAccessor _httpContextAccessor;

    public DevelopmentRequestContext(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public Guid TenantId => TenantIdValue;
    public Guid UserId =>
        Guid.TryParse(
            _httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier),
            out var userId)
            ? userId
            : AgentUserIdValue;
    public Guid CustomerId => CustomerIdValue;
}
