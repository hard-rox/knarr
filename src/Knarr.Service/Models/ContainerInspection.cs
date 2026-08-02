namespace Knarr.Service.Models;

/// <summary>Shaped result of a <c>container inspect</c> invocation. All collections are never null.</summary>
public sealed record ContainerInspection
{
    public required string Id { get; init; }
    public string ShortId { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Image { get; init; } = string.Empty;
    public DateTimeOffset? Created { get; init; }
    public string Status { get; init; } = string.Empty;
    public bool IsRunning { get; init; }
    public int ExitCode { get; init; }
    public DateTimeOffset? StartedAt { get; init; }
    public DateTimeOffset? FinishedAt { get; init; }
    public IReadOnlyList<string> Entrypoint { get; init; } = [];
    public IReadOnlyList<string> Command { get; init; } = [];
    public string WorkingDirectory { get; init; } = string.Empty;
    public string User { get; init; } = string.Empty;
    public IReadOnlyList<InspectionEntry> EnvironmentVariables { get; init; } = [];
    public IReadOnlyList<InspectionEntry> Labels { get; init; } = [];
    /// <summary>Port bindings formatted as <c>proto/port → host:port</c>.</summary>
    public IReadOnlyList<string> Ports { get; init; } = [];
    public IReadOnlyList<ContainerNetworkInfo> Networks { get; init; } = [];
    public string NetworkMode { get; init; } = string.Empty;
    public long MemoryBytes { get; init; }
    public long NanoCpus { get; init; }
    public string RawJson { get; init; } = string.Empty;
}

public sealed record ContainerNetworkInfo(
    string Name,
    string Gateway,
    string IPAddress,
    int IPPrefixLen,
    string MacAddress,
    IReadOnlyList<string> Aliases);
