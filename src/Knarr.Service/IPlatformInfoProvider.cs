namespace Knarr.Service;

/// <summary>
/// Supplies information about the host platform and its container CLI for display
/// in the UI. The container CLI version is detected at runtime by invoking the CLI.
/// </summary>
public interface IPlatformInfoProvider
{
    /// <summary>Friendly OS name, e.g. "macOS" or "Windows".</summary>
    string PlatformName { get; }

    /// <summary>Display name of the container CLI, e.g. "apple container" or "wslc".</summary>
    string CliName { get; }

    /// <summary>Detected CLI version (e.g. "v1.0.0"), or a placeholder before/after a failed probe.</summary>
    string CliVersion { get; }

    /// <summary>Whether the container CLI executed successfully during the last probe.</summary>
    bool IsCliReachable { get; }

    /// <summary>
    /// Runs the container CLI to refresh <see cref="CliVersion"/> and <see cref="IsCliReachable"/>.
    /// Never throws; failures leave the CLI reported as unreachable.
    /// </summary>
    Task RefreshCliInfoAsync(CancellationToken cancellationToken = default);
}
