using System.Text.Json;

namespace Knarr.Service.WslcCli;

internal sealed record ContainerResponse(
    long CreatedAt,
    string Id,
    string Image,
    string Name,
    List<PortResponse> Ports,
    int State,
    long StateChangedAt);

internal sealed record PortResponse(
    string BindingAddress,
    int ContainerPort,
    int HostPort,
    int Protocol);

internal sealed record ImageResponse(
    long Created,
    string Id,
    string Repository,
    long Size,
    string Tag);

// Shape emitted by `wslc image inspect <image>` (single-element OCI image config array).
internal sealed record ImageInspectResponse(
    string? Architecture,
    string? Author,
    string? Comment,
    ImageInspectConfigResponse? Config,
    DateTimeOffset? Created,
    string? Id,
    string? Os,
    string? Parent,
    List<string>? RepoDigests,
    List<string>? RepoTags,
    ImageInspectRootFsResponse? RootFS,
    long Size);

internal sealed record ImageInspectConfigResponse(
    List<string>? Cmd,
    List<string>? Entrypoint,
    List<string>? Env,
    Dictionary<string, JsonElement>? ExposedPorts,
    Dictionary<string, string>? Labels,
    string? StopSignal,
    string? User,
    Dictionary<string, JsonElement>? Volumes,
    string? WorkingDir);

internal sealed record ImageInspectRootFsResponse(
    List<string>? Layers,
    string? Type);

// Shape emitted by `wslc container inspect <id>`.
internal sealed record ContainerInspectResponse(
    ContainerInspectConfigResponse? Config,
    DateTimeOffset? Created,
    ContainerHostConfigResponse? HostConfig,
    string? Id,
    string? Image,
    Dictionary<string, string>? Labels,
    List<JsonElement>? Mounts,
    string? Name,
    ContainerNetworkSettingsResponse? NetworkSettings,
    Dictionary<string, List<ContainerPortBindingResponse>>? Ports,
    ContainerInspectStateResponse? State);

internal sealed record ContainerInspectConfigResponse(
    List<string>? Cmd,
    List<string>? Entrypoint,
    List<string>? Env,
    string? User,
    string? WorkingDir);

internal sealed record ContainerHostConfigResponse(
    long Memory,
    long NanoCpus,
    string? NetworkMode);

internal sealed record ContainerInspectStateResponse(
    int ExitCode,
    DateTimeOffset? FinishedAt,
    bool Running,
    DateTimeOffset? StartedAt,
    string? Status);

internal sealed record ContainerNetworkSettingsResponse(
    Dictionary<string, ContainerNetworkEntryResponse>? Networks);

internal sealed record ContainerNetworkEntryResponse(
    List<string>? Aliases,
    string? Gateway,
    string? IPAddress,
    int IPPrefixLen,
    string? MacAddress);

internal sealed record ContainerPortBindingResponse(
    string? HostIp,
    string? HostPort);
