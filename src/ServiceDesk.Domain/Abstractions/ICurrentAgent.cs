namespace ServiceDesk.Domain.Abstractions;

public interface ICurrentAgent
{
    Guid UserId { get; }
}
