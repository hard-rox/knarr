using Knarr.Service.Exceptions;

namespace Knarr.Service;

/// <summary>
/// Platform-agnostic abstraction over the host's first-party container CLI. Every method maps 1:1
/// onto a single CLI invocation so that GUI actions remain a thin, auditable pass-through to the
/// underlying runtime. Results are shaped, UI-ready records the app layer consumes without further
/// processing.
/// </summary>
public interface IContainerCliProvider
{
    /// <summary>
    /// Whether this platform CLI supports <c>run --publish-all</c>.
    /// </summary>
    public bool SupportsPublishAllPorts { get; }

    /// <summary>
    /// Whether this platform CLI supports <c>logs --timestamps</c>.
    /// </summary>
    public bool SupportsLogTimestamps { get; }

    /// <summary>
    /// Whether this platform CLI supports <c>logs --since</c>/<c>--until</c> filtering.
    /// </summary>
    public bool SupportsLogTimeRange { get; }

    /// <summary>
    /// Whether this platform CLI supports <c>logs --boot</c> (the container's boot log).
    /// </summary>
    public bool SupportsBootLogs { get; }

    /// <summary>Lists all containers (<c>list --all --format JSON</c>).</summary>
    public Task<IReadOnlyList<Container>> ListContainersAsync(CancellationToken cancellationToken = default);

    /// <summary>Starts a stopped container (<c>start</c>).</summary>
    public Task StartContainerAsync(string id, CancellationToken cancellationToken = default);

    /// <summary>Restarts a container (a <c>stop</c> followed by a <c>start</c>).</summary>
    public Task RestartContainerAsync(string id, CancellationToken cancellationToken = default);

    /// <summary>Gracefully stops a running container (<c>stop</c>).</summary>
    public Task StopContainerAsync(string id, CancellationToken cancellationToken = default);

    /// <summary>Removes a container (<c>remove</c>), optionally forcing a running one.</summary>
    public Task RemoveContainerAsync(string id, bool force = false, CancellationToken cancellationToken = default);

    /// <summary>
    /// Starts several containers. The <c>start</c> command takes a single container, so this loops
    /// over <paramref name="ids"/> and aggregates any per-container failures into a single
    /// <see cref="AggregateCliCommandException"/>. No-ops on an empty list.
    /// </summary>
    public Task StartContainersAsync(IReadOnlyList<string> ids, CancellationToken cancellationToken = default);

    /// <summary>
    /// Stops several containers in a single <c>stop</c> invocation (the CLI accepts multiple ids).
    /// No-ops on an empty list.
    /// </summary>
    public Task StopContainersAsync(IReadOnlyList<string> ids, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes several containers in a single <c>remove</c> invocation (the CLI accepts multiple
    /// ids), optionally forcing running ones. No-ops on an empty list.
    /// </summary>
    public Task RemoveContainersAsync(IReadOnlyList<string> ids, bool force = false, CancellationToken cancellationToken = default);

    /// <summary>
    /// Builds the exact <c>run</c> command line that <paramref name="options"/> would execute,
    /// without running anything. Used to surface a live, read-only command preview (transparency).
    /// </summary>
    public string BuildRunContainerCommand(RunContainerOptions options);

    /// <summary>
    /// Runs a container from an image (<c>run</c>) using <paramref name="options"/>, returning the
    /// buffered standard output (typically the new container id). Intended for detached runs that
    /// return immediately.
    /// </summary>
    public Task<string> RunContainerAsync(RunContainerOptions options, CancellationToken cancellationToken = default);

    /// <summary>
    /// Runs a container from an image (<c>run</c>) using <paramref name="options"/>, streaming the
    /// command transcript line-by-line. The first emitted line is the exact command executed
    /// (transparency); stdout and stderr lines follow in arrival order; a final
    /// <see cref="CliOutputKind.Exit"/> line carries the exit code. Intended for foreground
    /// (non-detached) runs so live logs can be surfaced. Cancellation is cooperative.
    /// </summary>
    public IAsyncEnumerable<CliOutputLine> RunContainerStreamingAsync(RunContainerOptions options, CancellationToken cancellationToken = default);

    /// <summary>
    /// Streams a container's logs (<c>logs</c>) using <paramref name="options"/>, emitting each line
    /// line-by-line as it arrives. The first emitted line is the exact command executed
    /// (transparency); stdout and stderr lines follow in arrival order; a final
    /// <see cref="CliOutputKind.Exit"/> line carries the exit code (omitted while following, since
    /// the process only exits when cancelled). Cancellation is cooperative and is the only way to
    /// stop a follow (<c>--follow</c>) stream.
    /// </summary>
    public IAsyncEnumerable<CliOutputLine> StreamContainerLogsAsync(ContainerLogsOptions options, CancellationToken cancellationToken = default);

    /// <summary>Lists local images (<c>images --format JSON</c>).</summary>
    public Task<IReadOnlyList<ContainerImage>> ListImagesAsync(CancellationToken cancellationToken = default);

    /// <summary>Pulls an image from a registry (<c>pull</c>).</summary>
    public Task PullImageAsync(string reference, CancellationToken cancellationToken = default);

    /// <summary>
    /// Pulls an image from a registry (<c>pull</c>), streaming the command transcript line-by-line as
    /// the process runs. The first emitted line is the exact command executed (transparency); stdout
    /// and stderr lines follow in arrival order; a final <see cref="CliOutputKind.Exit"/> line carries
    /// the exit code. Cancellation is cooperative: <paramref name="cancellationToken"/> requests a
    /// graceful stop (interrupt signal) with a forceful kill as a fallback. A non-zero exit is
    /// reported via the terminating <see cref="CliOutputKind.Exit"/> line rather than by throwing.
    /// </summary>
    public IAsyncEnumerable<CliOutputLine> PullImageStreamingAsync(string reference, CancellationToken cancellationToken = default);

    /// <summary>Removes an image (<c>rmi</c>), optionally forcing.</summary>
    public Task RemoveImageAsync(string reference, bool force = false, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes several images in a single <c>rmi</c> invocation (the CLI accepts multiple
    /// references), optionally forcing. No-ops on an empty list.
    /// </summary>
    public Task RemoveImagesAsync(IReadOnlyList<string> references, bool force = false, CancellationToken cancellationToken = default);

    /// <summary>Probes the CLI (<c>--version</c>) for platform and version info. Never throws.</summary>
    public Task<PlatformInfo> GetPlatformInfoAsync(CancellationToken cancellationToken = default);
}
