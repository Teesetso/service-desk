namespace ServiceDesk.Infrastructure.Portal;

public sealed class CatalogueItemUnavailableException : Exception
{
    public CatalogueItemUnavailableException(string message)
        : base(message)
    {
    }
}
