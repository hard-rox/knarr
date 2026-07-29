namespace Knarr.Service.Models;

public sealed record ContainerImage
{
    public required string Repository { get; init; }

    public required string Tag { get; init; }

    public required string Id { get; init; }

    public DateTimeOffset Created { get; init; }

    public string Size { get; init; } = "\u2014";
}
