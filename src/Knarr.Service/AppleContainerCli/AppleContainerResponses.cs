namespace Knarr.Service.AppleContainerCli;

// Deserialization shapes for the Apple `container` CLI JSON output. These mirror the nested
// structure emitted by `container list --format json`, `container image list --format json`,
// `container inspect`, and `container image inspect`
// (Web serializer defaults: camelCase, case-insensitive; ISO 8601 dates -> DateTimeOffset).

internal sealed record AppleContainerElement(
    AppleContainerConfiguration Configuration,
    string Id,
    AppleContainerStatus Status);

internal sealed record AppleContainerConfiguration(
    DateTimeOffset CreationDate,
    string Id,
    AppleImageReference Image,
    List<ApplePublishedPort>? PublishedPorts,
    AppleInitProcess? InitProcess,
    AppleContainerResources? Resources);

internal sealed record AppleImageReference(string Reference);

internal sealed record AppleContainerStatus(
    DateTimeOffset? StartedDate,
    string State,
    List<AppleContainerStatusNetwork>? Networks);

internal sealed record AppleContainerStatusNetwork(
    string? Hostname,
    string? Ipv4Address,
    string? Ipv4Gateway,
    string? MacAddress,
    string? Network);

internal sealed record AppleInitProcess(
    string? Executable,
    List<string>? Arguments,
    List<string>? Environment,
    string? WorkingDirectory);

internal sealed record AppleContainerResources(
    double Cpus,
    long MemoryInBytes);

internal sealed record ApplePublishedPort(
    string? HostAddress,
    int HostPort,
    int ContainerPort,
    string? Proto);

internal sealed record AppleImageElement(
    AppleImageConfiguration Configuration,
    string Id,
    List<AppleImageVariant>? Variants);

internal sealed record AppleImageConfiguration(
    DateTimeOffset CreationDate,
    string Name,
    AppleImageDescriptor? Descriptor);

internal sealed record AppleImageDescriptor(
    string? Digest,
    string? MediaType,
    long Size);

internal sealed record AppleImageVariant(
    long Size,
    AppleImageVariantConfig? Config,
    string? Digest,
    ApplePlatform? Platform);

internal sealed record AppleImageVariantConfig(
    string? Architecture,
    AppleImageVariantInnerConfig? Config,
    string? Os);

internal sealed record AppleImageVariantInnerConfig(
    List<string>? Cmd,
    List<string>? Entrypoint,
    List<string>? Env,
    string? StopSignal);

internal sealed record ApplePlatform(
    string? Architecture,
    string? Os);

// Shape emitted by `container system status --format json` (a single object, not an array).
internal sealed record AppleSystemStatusResponse(
    string? ApiServerVersion,
    string? AppRoot,
    string? InstallRoot,
    string? Status);
