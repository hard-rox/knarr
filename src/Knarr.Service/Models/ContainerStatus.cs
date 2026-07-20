namespace Knarr.Service.Models;

/// <summary>Lifecycle state of a container as reported by the underlying CLI.</summary>
public enum ContainerStatus
{
    Created,
    Running,
    Paused,
    Exited
}
