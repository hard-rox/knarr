namespace Knarr.Service.Models;

/// <summary>
/// Lifecycle state of the host's container system services, as reported by
/// <c>container system status</c>.
/// </summary>
public enum ContainerSystemState
{
    /// <summary>The state could not be determined (CLI missing, unparsable output, …).</summary>
    Unknown,

    /// <summary>The API server responded to the health check; the system is up.</summary>
    Running,

    /// <summary>The services are not registered with launchd; the system has never been started.</summary>
    Unregistered,

    /// <summary>The services are registered but not currently running.</summary>
    NotRunning,
}

/// <summary>
/// Shaped result of <c>container system status --format json</c>, describing the container system
/// services and the API server backing them.
/// </summary>
public sealed record ContainerSystemStatus
{
    /// <summary>Fallback used when the status could not be determined. Never surfaced as an error.</summary>
    public static ContainerSystemStatus Unknown { get; } = new();

    /// <summary>Current lifecycle state of the system services.</summary>
    public ContainerSystemState State { get; init; } = ContainerSystemState.Unknown;

    /// <summary>Full API server version string, or empty when the system is not running.</summary>
    public string ApiServerVersion { get; init; } = string.Empty;

    /// <summary>Root directory for application data, or empty when the system is not running.</summary>
    public string AppRoot { get; init; } = string.Empty;

    /// <summary>Root directory for application executables, or empty when the system is not running.</summary>
    public string InstallRoot { get; init; } = string.Empty;
}
