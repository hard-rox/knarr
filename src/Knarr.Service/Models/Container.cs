namespace Knarr.Service.Models;

/// <summary>Lifecycle state of a container, mapped from the CLI's numeric state.</summary>
public enum ContainerState
{
    Unknown,
    Created,
    Running,
    Paused,
    Exited,
}

/// <summary>
/// A shaped, UI-ready snapshot of a single container returned by <see cref="IContainerCliProvider"/>.
/// The app layer consumes this directly: the id is already shortened and ports are pre-formatted.
/// </summary>
public sealed record Container
{
    /// <summary>Short 12-character id, matching the CLI's abbreviated form.</summary>
    public required string Id { get; init; }

    public required string Name { get; init; }

    public required string Image { get; init; }

    public ContainerState State { get; init; }

    /// <summary>Display-ready port mappings, e.g. "6379→6379/tcp", or an em dash when none.</summary>
    public string Ports { get; init; } = "\u2014";

    public DateTimeOffset CreatedAt { get; init; }

    public DateTimeOffset StateChangedAt { get; init; }
}
