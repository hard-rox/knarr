namespace Knarr.Service.Models;

public sealed record RunContainerOptions
{
    public required string ImageReference { get; init; }

    public bool Detach { get; init; } = true;

    public bool RemoveOnExit { get; init; }

    public string? Name { get; init; }

    public IReadOnlyList<RunEnvironmentVariable> EnvironmentVariables { get; init; } = [];

    public IReadOnlyList<RunVolumeMount> Volumes { get; init; } = [];

    public IReadOnlyList<RunPortMapping> Ports { get; init; } = [];

    public bool PublishAllPorts { get; init; }
}

public sealed record RunEnvironmentVariable(string Key, string Value);

public sealed record RunVolumeMount(string Source, string Target);

public sealed record RunPortMapping(string HostPort, string ContainerPort);
