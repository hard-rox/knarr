using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Knarr.App.Models;

namespace Knarr.App.Services;

/// <summary>
/// Shared plumbing for the concrete container CLI providers: runs the executable via
/// <see cref="ICliProcessRunner"/>, throws <see cref="CliCommandException"/> on failure, and
/// provides parsing helpers used by both platforms. Subclasses supply the executable name, the
/// exact argument lists for each verb, and the JSON shape of their list output.
/// </summary>
public abstract class CliProviderBase : IContainerCliProvider
{
    private readonly ICliProcessRunner _runner;

    protected CliProviderBase(ICliProcessRunner runner) => _runner = runner;

    /// <summary>Name (or path) of the CLI executable, e.g. <c>container</c> or <c>wslc</c>.</summary>
    protected abstract string Executable { get; }

    // ----- Containers -----

    public abstract Task<IReadOnlyList<ContainerSummary>> ListContainersAsync(
        bool includeAll = true,
        CancellationToken cancellationToken = default);

    public abstract Task<IReadOnlyList<ImageSummary>> ListImagesAsync(
        CancellationToken cancellationToken = default);

    public Task StartContainerAsync(string id, CancellationToken cancellationToken = default)
        => RunAsync(cancellationToken, "start", id);

    public Task StopContainerAsync(string id, CancellationToken cancellationToken = default)
        => RunAsync(cancellationToken, "stop", id);

    public async Task RestartContainerAsync(string id, CancellationToken cancellationToken = default)
    {
        // Neither CLI is guaranteed a native restart verb, so compose stop + start.
        await StopContainerAsync(id, cancellationToken).ConfigureAwait(false);
        await StartContainerAsync(id, cancellationToken).ConfigureAwait(false);
    }

    public Task KillContainerAsync(string id, CancellationToken cancellationToken = default)
        => RunAsync(cancellationToken, "kill", id);

    public abstract Task RemoveContainerAsync(
        string id,
        bool force = false,
        CancellationToken cancellationToken = default);

    public Task<string> InspectContainerAsync(string id, CancellationToken cancellationToken = default)
        => CaptureAsync(cancellationToken, "inspect", id);

    // ----- Images -----

    public abstract Task PullImageAsync(string reference, CancellationToken cancellationToken = default);

    public abstract Task PushImageAsync(string reference, CancellationToken cancellationToken = default);

    public abstract Task TagImageAsync(string source, string target, CancellationToken cancellationToken = default);

    public abstract Task RemoveImageAsync(
        string reference,
        bool force = false,
        CancellationToken cancellationToken = default);

    public abstract Task PruneImagesAsync(CancellationToken cancellationToken = default);

    public abstract Task<string> InspectImageAsync(string reference, CancellationToken cancellationToken = default);

    // ----- Execution helpers -----

    /// <summary>Runs the CLI and throws <see cref="CliCommandException"/> on a non-zero exit.</summary>
    protected async Task RunAsync(CancellationToken cancellationToken, params string[] arguments)
    {
        await CaptureAsync(cancellationToken, arguments).ConfigureAwait(false);
    }

    /// <summary>Runs the CLI, returning stdout, and throws <see cref="CliCommandException"/> on failure.</summary>
    protected async Task<string> CaptureAsync(CancellationToken cancellationToken, params string[] arguments)
    {
        var result = await _runner.RunAsync(Executable, arguments, cancellationToken).ConfigureAwait(false);
        if (!result.Succeeded)
        {
            var command = $"{Executable} {string.Join(' ', arguments)}";
            throw new CliCommandException(command, result.ExitCode, result.StandardError);
        }

        return result.StandardOutput;
    }

    // ----- Parsing helpers -----

    /// <summary>Maps a CLI status string (e.g. "running", "exited") onto <see cref="ContainerStatus"/>.</summary>
    protected static ContainerStatus ParseStatus(string? status) => status?.Trim().ToLowerInvariant() switch
    {
        "running" => ContainerStatus.Running,
        "paused" => ContainerStatus.Paused,
        "created" => ContainerStatus.Created,
        _ => ContainerStatus.Exited,
    };

    /// <summary>Splits an image reference ("repo:tag" or "repo") into repository and tag parts.</summary>
    protected static (string Repository, string Tag) SplitReference(string reference)
    {
        var repo = reference?.Trim() ?? string.Empty;
        var lastSlash = repo.LastIndexOf('/');
        var lastColon = repo.LastIndexOf(':');

        // A colon that precedes the final path segment is a port, not a tag separator.
        if (lastColon > lastSlash && lastColon >= 0)
        {
            return (repo[..lastColon], repo[(lastColon + 1)..]);
        }

        return (repo, "latest");
    }

    /// <summary>Formats a raw byte count as a human-readable size (MB/GB), matching CLI-style output.</summary>
    protected static string FormatSize(long bytes)
    {
        if (bytes <= 0)
        {
            return "\u2014";
        }

        string[] units = ["B", "KB", "MB", "GB", "TB"];
        double size = bytes;
        var unit = 0;
        while (size >= 1024 && unit < units.Length - 1)
        {
            size /= 1024;
            unit++;
        }

        return $"{size.ToString(size >= 100 ? "0" : "0.0", CultureInfo.InvariantCulture)} {units[unit]}";
    }
}
