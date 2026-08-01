using Knarr.Service.Exceptions;

namespace Knarr.Service;

/// <summary>
/// Controls the host's container system services (the background API server the CLI talks to).
/// This capability is macOS-only: it is registered in DI exclusively on macOS, so consumers must
/// resolve it optionally (<c>GetService</c>) and hide the corresponding UI when it is absent.
/// Every method maps 1:1 onto a single <c>container system</c> invocation.
/// </summary>
public interface IContainerSystemService
{
    /// <summary>
    /// Reads the current system status (<c>system status --format json</c>). Never throws: an
    /// unreachable CLI or unparsable payload yields <see cref="ContainerSystemStatus.Unknown"/>.
    /// </summary>
    public Task<ContainerSystemStatus> GetStatusAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Starts the container system services (<c>system start</c>). Throws
    /// <see cref="CliCommandException"/> when the command exits non-zero.
    /// </summary>
    public Task StartAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Stops the container system services and deregisters them (<c>system stop</c>). Throws
    /// <see cref="CliCommandException"/> when the command exits non-zero.
    /// </summary>
    public Task StopAsync(CancellationToken cancellationToken = default);
}
