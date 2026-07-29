namespace Knarr.Service.Models;

public enum ContainerState
{
    Unknown,
    Created,
    Running,
    Paused,
    Exited,
}

public sealed record Container
{
    public required string Id { get; init; }

    public required string Name { get; init; }

    public required string Image { get; init; }

    public ContainerState State { get; init; }

    public string Ports { get; init; } = "\u2014";

    public DateTimeOffset CreatedAt { get; init; }

    public DateTimeOffset StateChangedAt { get; init; }
}
