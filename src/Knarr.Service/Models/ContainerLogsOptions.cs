namespace Knarr.Service.Models;

/// <summary>
/// The options for a single container logs (<c>logs</c>) invocation. Shaped by the app layer and
/// translated 1:1 into CLI arguments by the provider so the executed command stays auditable.
/// </summary>
public sealed record ContainerLogsOptions
{
    /// <summary>The id of the container whose logs are streamed.</summary>
    public required string ContainerId { get; init; }

    /// <summary>When true, continues streaming new output as it is produced (<c>--follow</c>).</summary>
    public bool Follow { get; init; }

    /// <summary>When true, prefixes each log line with its timestamp (<c>--timestamps</c>).</summary>
    public bool Timestamps { get; init; }

    /// <summary>When set, limits output to the last N lines (<c>--tail N</c>); omitted when null.</summary>
    public int? TailLines { get; init; }

    /// <summary>When set, shows logs produced at or after this instant (<c>--since</c>); omitted when null.</summary>
    public DateTimeOffset? Since { get; init; }

    /// <summary>When set, shows logs produced before this instant (<c>--until</c>); omitted when null.</summary>
    public DateTimeOffset? Until { get; init; }
}
