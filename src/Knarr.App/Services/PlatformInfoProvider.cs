using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace Knarr.App.Services;

/// <summary>
/// Detects the host OS and maps it to the corresponding first-party container CLI,
/// then probes that CLI at runtime for its version. Backend health remains stubbed
/// until <c>IContainerCliProvider</c> is implemented.
/// </summary>
public sealed partial class PlatformInfoProvider : IPlatformInfoProvider
{
    private const string UnknownVersion = "not detected";

    private readonly string _cliExecutable;

    public PlatformInfoProvider()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            PlatformName = "macOS";
            CliName = "apple container";
            _cliExecutable = "container";
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            PlatformName = "Windows";
            CliName = "wslc";
            _cliExecutable = "wslc";
        }
        else
        {
            PlatformName = "Linux";
            CliName = "docker";
            _cliExecutable = "docker";
        }
    }

    public string PlatformName { get; }

    public string CliName { get; }

    public string CliVersion { get; private set; } = UnknownVersion;

    public bool IsCliReachable { get; private set; }

    public async Task RefreshCliInfoAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = _cliExecutable,
                Arguments = "--version",
                RedirectStandardOutput = true,
                RedirectStandardError = false,
                UseShellExecute = false,
                CreateNoWindow = true,
            };

            using var process = Process.Start(startInfo);
            if (process is null)
            {
                SetUnreachable();
                return;
            }

            var output = await process.StandardOutput.ReadToEndAsync(cancellationToken).ConfigureAwait(false);
            await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);

            if (process.ExitCode != 0)
            {
                SetUnreachable();
                return;
            }

            CliVersion = ParseVersion(output);
            IsCliReachable = true;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // CLI missing from PATH, not executable, etc. — treat as unreachable.
            SetUnreachable();
        }
    }

    private void SetUnreachable()
    {
        CliVersion = UnknownVersion;
        IsCliReachable = false;
    }

    /// <summary>
    /// Extracts a semantic version (e.g. "1.0.0") from CLI output such as
    /// "container CLI version 1.0.0 (build: release, commit: ee848e3)" and prefixes it with "v".
    /// Falls back to the first non-empty output line when no version number is found.
    /// </summary>
    private static string ParseVersion(string output)
    {
        var match = VersionRegex().Match(output);
        if (match.Success)
        {
            return $"v{match.Value}";
        }

        var firstLine = output
            .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .FirstOrDefault();

        return string.IsNullOrEmpty(firstLine) ? UnknownVersion : firstLine;
    }

    [GeneratedRegex(@"\d+\.\d+\.\d+")]
    private static partial Regex VersionRegex();
}
