namespace Knarr.Service;

/// <summary>
/// Platform-agnostic abstraction over the host's first-party container CLI (<c>container</c> on
/// macOS, <c>wslc</c> on Windows). Every method maps 1:1 onto a single CLI invocation so that GUI
/// actions remain a thin, auditable pass-through to the underlying runtime.
/// </summary>
public interface IContainerCliProvider
{
    // ----- Containers -----

    /// <summary>Lists containers (<c>list</c>). When <paramref name="includeAll"/> is false, only running ones.</summary>
    Task<IReadOnlyList<ContainerSummary>> ListContainersAsync(
        bool includeAll = true,
        CancellationToken cancellationToken = default);

    /// <summary>Starts a stopped container (<c>start</c>).</summary>
    Task StartContainerAsync(string id, CancellationToken cancellationToken = default);

    /// <summary>Restarts a container. Apple's CLI has no <c>restart</c>, so this is a stop then start.</summary>
    Task RestartContainerAsync(string id, CancellationToken cancellationToken = default);

    /// <summary>Gracefully stops a running container (<c>stop</c>).</summary>
    Task StopContainerAsync(string id, CancellationToken cancellationToken = default);

    /// <summary>Removes a container (<c>remove</c>/<c>delete</c>), optionally forcing a running one.</summary>
    Task RemoveContainerAsync(string id, bool force = false, CancellationToken cancellationToken = default);

    // ----- Images -----

    /// <summary>Lists local images (<c>image list</c>/<c>images</c>).</summary>
    Task<IReadOnlyList<ImageSummary>> ListImagesAsync(CancellationToken cancellationToken = default);

    /// <summary>Pulls an image from a registry (<c>pull</c>).</summary>
    Task PullImageAsync(string reference, CancellationToken cancellationToken = default);

    /// <summary>Removes an image (<c>rmi</c>/<c>image delete</c>), optionally forcing.</summary>
    Task RemoveImageAsync(string reference, bool force = false, CancellationToken cancellationToken = default);
}
