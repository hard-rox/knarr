namespace Knarr.Service.Models;

/// <summary>
/// Information about the host platform and its container CLI, returned by
/// <see cref="IContainerCliProvider.GetPlatformInfoAsync"/> for display in the UI.
/// </summary>
public sealed record PlatformInfo
{
    /// <summary>Friendly OS name, e.g. "Windows".</summary>
    public required string PlatformName { get; init; }

    /// <summary>Display name of the container CLI, e.g. "wslc".</summary>
    public required string CliName { get; init; }

    /// <summary>Detected CLI version (e.g. "v2.9.3.0"), or a placeholder when the probe failed.</summary>
    public required string CliVersion { get; init; }

    /// <summary>Whether the container CLI executed successfully during the probe.</summary>
    public bool IsCliReachable { get; init; }
}
