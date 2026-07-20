using System.Runtime.InteropServices;

namespace Knarr.Service;

/// <summary>
/// A no-op <see cref="IPlatformInfoProvider"/> that reports the host OS name and a placeholder CLI
/// version without probing anything. Used by the Avalonia previewer so design-time view models can
/// surface platform information without the real (internal) provider.
/// </summary>
public sealed class DesignTimePlatformInfoProvider : IPlatformInfoProvider
{
    public DesignTimePlatformInfoProvider()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            PlatformName = "macOS";
            CliName = "apple container";
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            PlatformName = "Windows";
            CliName = "wslc";
        }
        else
        {
            PlatformName = "Linux";
            CliName = "docker";
        }
    }

    public string PlatformName { get; }

    public string CliName { get; }

    public string CliVersion => "v1.0.0";

    public bool IsCliReachable => true;

    public Task RefreshCliInfoAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
}
