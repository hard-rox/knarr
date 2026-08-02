namespace Knarr.Service.Models;

/// <summary>Shaped result of an <c>image inspect</c> invocation. All collections are never null.</summary>
public sealed record ImageInspection
{
    public required string Id { get; init; }
    public string ShortId { get; init; } = string.Empty;
    public IReadOnlyList<string> RepoTags { get; init; } = [];
    public IReadOnlyList<string> RepoDigests { get; init; } = [];
    public string Architecture { get; init; } = string.Empty;
    public string Os { get; init; } = string.Empty;
    public long SizeBytes { get; init; }
    public string Size { get; init; } = "\u2014";
    public DateTimeOffset? Created { get; init; }
    public string Author { get; init; } = string.Empty;
    public string Comment { get; init; } = string.Empty;
    public string Parent { get; init; } = string.Empty;
    public IReadOnlyList<string> Entrypoint { get; init; } = [];
    public IReadOnlyList<string> Command { get; init; } = [];
    public string WorkingDirectory { get; init; } = string.Empty;
    public string User { get; init; } = string.Empty;
    public string StopSignal { get; init; } = string.Empty;
    public IReadOnlyList<InspectionEntry> EnvironmentVariables { get; init; } = [];
    public IReadOnlyList<InspectionEntry> Labels { get; init; } = [];
    /// <summary>Exposed ports in <c>port/protocol</c> form (e.g. "6379/tcp"), sorted numerically.</summary>
    public IReadOnlyList<string> ExposedPorts { get; init; } = [];
    public IReadOnlyList<string> Volumes { get; init; } = [];
    public string RawJson { get; init; } = string.Empty;
}
