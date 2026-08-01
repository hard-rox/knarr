namespace Knarr.Service.Models;

public enum ContainerSystemState
{
    Unknown,

    Running,

    Unregistered,

    NotRunning,
}

public sealed record ContainerSystemStatus
{
    public static ContainerSystemStatus Unknown { get; } = new();

    public ContainerSystemState State { get; init; } = ContainerSystemState.Unknown;

    public string ApiServerVersion { get; init; } = string.Empty;

    public string AppRoot { get; init; } = string.Empty;

    public string InstallRoot { get; init; } = string.Empty;
}
