namespace Knarr.Service.Models;

/// <summary>
/// The options for a single container run (<c>run</c>) invocation. Shaped by the app layer and
/// translated 1:1 into CLI arguments by the provider so the executed command stays auditable.
/// </summary>
public sealed record RunContainerOptions
{
    /// <summary>The image reference to run (e.g. <c>docker.io/library/alpine:3.20</c>).</summary>
    public required string ImageReference { get; init; }

    /// <summary>When true, runs the container in the background (<c>--detach</c>). Defaults to true.</summary>
    public bool Detach { get; init; } = true;

    /// <summary>When true, removes the container automatically after it exits (<c>--rm</c>).</summary>
    public bool RemoveOnExit { get; init; }

    /// <summary>Optional container name (<c>--name</c>); omitted when null or whitespace.</summary>
    public string? Name { get; init; }

    /// <summary>Environment variables passed as <c>--env KEY=VALUE</c>, in order.</summary>
    public IReadOnlyList<RunEnvironmentVariable> EnvironmentVariables { get; init; } = [];

    /// <summary>Volume mounts passed as <c>--volume SOURCE:TARGET</c>, in order.</summary>
    public IReadOnlyList<RunVolumeMount> Volumes { get; init; } = [];

    /// <summary>Port mappings passed as <c>--publish HOST:CONTAINER</c>, in order.</summary>
    public IReadOnlyList<RunPortMapping> Ports { get; init; } = [];

    /// <summary>When true, publishes all exposed ports to random host ports (<c>--publish-all</c>).</summary>
    public bool PublishAllPorts { get; init; }
}

/// <summary>A single environment variable passed to a container run as <c>--env KEY=VALUE</c>.</summary>
/// <param name="Key">The variable name.</param>
/// <param name="Value">The variable value.</param>
public sealed record RunEnvironmentVariable(string Key, string Value);

/// <summary>A single bind volume mount passed to a container run as <c>--volume SOURCE:TARGET</c>.</summary>
/// <param name="Source">The host path (or named volume).</param>
/// <param name="Target">The in-container mount path.</param>
public sealed record RunVolumeMount(string Source, string Target);

/// <summary>A single port mapping passed to a container run as <c>--publish HOST:CONTAINER</c>.</summary>
/// <param name="HostPort">The port published on the host.</param>
/// <param name="ContainerPort">The port exposed inside the container.</param>
public sealed record RunPortMapping(string HostPort, string ContainerPort);
