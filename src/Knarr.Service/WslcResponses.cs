namespace Knarr.Service;

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
