namespace Knarr.Service.Models;

/// <summary>
/// A UI-agnostic snapshot of a single container as reported by the underlying CLI's list command.
/// Returned by <see cref="IContainerCliProvider"/>; view models map this onto their own display
/// types. Fields the CLI does not surface in its list output default to an em dash.
/// </summary>
public sealed record ContainerSummary
{
    public required string Id { get; init; }

    public required string Name { get; init; }

    public required string Image { get; init; }

    public ContainerStatus Status { get; init; }

    public string Ports { get; init; } = "\u2014";

    public string Cpu { get; init; } = "\u2014";

    public string Memory { get; init; } = "\u2014";

    public string Uptime { get; init; } = "\u2014";
}
