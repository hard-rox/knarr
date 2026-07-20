namespace Knarr.App.Models;

/// <summary>
/// A UI-agnostic snapshot of a single image as reported by the underlying CLI's list command.
/// Returned by <c>IContainerCliProvider</c>; view models map this onto <see cref="ImageItem"/>
/// for display. Fields the CLI does not surface in its list output default to an em dash.
/// </summary>
public sealed record ImageSummary
{
    public required string Repository { get; init; }

    public required string Tag { get; init; }

    public required string Id { get; init; }

    public string Created { get; init; } = "\u2014";

    public string Size { get; init; } = "\u2014";
}
