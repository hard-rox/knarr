namespace Knarr.Service.Models;

public sealed record ContainerLogsOptions
{
    public required string ContainerId { get; init; }

    public bool Follow { get; init; }

    public bool Timestamps { get; init; }

    public int? TailLines { get; init; }

    public DateTimeOffset? Since { get; init; }

    public DateTimeOffset? Until { get; init; }
}
