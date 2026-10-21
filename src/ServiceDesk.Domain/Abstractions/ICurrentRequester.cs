namespace ServiceDesk.Domain.Abstractions;

public interface ICurrentRequester
{
    Guid UserId { get; }
    Guid CustomerId { get; }
}
