namespace Knarr.Service.Models;

/// <summary>
/// A shaped, UI-ready snapshot of a single local image returned by <see cref="IContainerCliProvider"/>.
/// The id is already shortened and the size is pre-formatted for display.
/// </summary>
public sealed record ContainerImage
{
    public required string Repository { get; init; }

    public required string Tag { get; init; }

    /// <summary>Short 12-character id, with any digest algorithm prefix (e.g. "sha256:") stripped.</summary>
    public required string Id { get; init; }

    public DateTimeOffset Created { get; init; }

    /// <summary>Human-readable size, e.g. "250 MB".</summary>
    public string Size { get; init; } = "\u2014";
}
