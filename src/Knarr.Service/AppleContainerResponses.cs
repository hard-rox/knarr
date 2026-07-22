namespace Knarr.Service;

// Deserialization shapes for the Apple `container` CLI JSON output. These mirror the nested
// structure emitted by `container list --format json` and `container image list --format json`
// (Web serializer defaults: camelCase, case-insensitive; ISO 8601 dates -> DateTimeOffset).

internal sealed record AppleContainerElement(
    AppleContainerConfiguration Configuration,
    string Id,
    AppleContainerStatus Status);

internal sealed record AppleContainerConfiguration(
    DateTimeOffset CreationDate,
    string Id,
    AppleImageReference Image,
    List<ApplePublishedPort>? PublishedPorts);

internal sealed record AppleImageReference(string Reference);

internal sealed record AppleContainerStatus(
    DateTimeOffset? StartedDate,
    string State);

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
    string Name);

internal sealed record AppleImageVariant(long Size);
