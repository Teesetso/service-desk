namespace ServiceDesk.Domain.Abstractions;

public interface ITenantContext
{
    Guid TenantId { get; }
}
