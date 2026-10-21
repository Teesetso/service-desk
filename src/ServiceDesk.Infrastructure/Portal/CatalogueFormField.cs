namespace ServiceDesk.Infrastructure.Portal;

public sealed class CatalogueFormField
{
    public string Name { get; init; } = string.Empty;
    public string Label { get; init; } = string.Empty;
    public string Type { get; init; } = "text";
    public bool Required { get; init; }
    public int MaxLength { get; init; } = 500;
    public IReadOnlyList<string> Options { get; init; } = Array.Empty<string>();
}
