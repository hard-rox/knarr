namespace Knarr.Service;

/// <summary>
/// Platform-agnostic abstraction over the host's first-party container CLI. Every method maps 1:1
/// onto a single CLI invocation so that GUI actions remain a thin, auditable pass-through to the
/// underlying runtime. Results are shaped, UI-ready records the app layer consumes without further
/// processing.
/// </summary>
public interface IContainerCliProvider
{
    // ----- Containers -----

    /// <summary>Lists all containers (<c>list --all --format JSON</c>).</summary>
    Task<IReadOnlyList<Container>> ListContainersAsync(CancellationToken cancellationToken = default);

    /// <summary>Starts a stopped container (<c>start</c>).</summary>
    Task StartContainerAsync(string id, CancellationToken cancellationToken = default);

    /// <summary>Restarts a container (a <c>stop</c> followed by a <c>start</c>).</summary>
    Task RestartContainerAsync(string id, CancellationToken cancellationToken = default);

    /// <summary>Gracefully stops a running container (<c>stop</c>).</summary>
    Task StopContainerAsync(string id, CancellationToken cancellationToken = default);

    /// <summary>Removes a container (<c>remove</c>), optionally forcing a running one.</summary>
    Task RemoveContainerAsync(string id, bool force = false, CancellationToken cancellationToken = default);

    // ----- Images -----

    /// <summary>Lists local images (<c>images --format JSON</c>).</summary>
    Task<IReadOnlyList<ContainerImage>> ListImagesAsync(CancellationToken cancellationToken = default);

    /// <summary>Pulls an image from a registry (<c>pull</c>).</summary>
    Task PullImageAsync(string reference, CancellationToken cancellationToken = default);

    /// <summary>Removes an image (<c>rmi</c>), optionally forcing.</summary>
    Task RemoveImageAsync(string reference, bool force = false, CancellationToken cancellationToken = default);

    // ----- Platform -----

    /// <summary>Probes the CLI (<c>--version</c>) for platform and version info. Never throws.</summary>
    Task<PlatformInfo> GetPlatformInfoAsync(CancellationToken cancellationToken = default);
}
