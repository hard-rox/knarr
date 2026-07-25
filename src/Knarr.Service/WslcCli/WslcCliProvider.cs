using System.Globalization;
using System.Linq;
using System.Text.Json;

namespace Knarr.Service.WslcCli;

/// <summary>
/// <see cref="IContainerCliProvider"/> backed by the <c>wslc</c> CLI on Windows. Supplies the
/// CLI-specific command verbs and JSON parsing; all process execution and formatting live in
/// <see cref="ContainerCliProviderBase"/>. Every method maps 1:1 onto a single <c>wslc</c> invocation.
/// </summary>
internal sealed class WslcCliProvider(ILogger<WslcCliProvider> logger)
    : ContainerCliProviderBase(logger)
{
    protected override string Executable => "wslc";

    protected override string PlatformName => "Windows";

    protected override string[] RemoveContainerCommand => ["remove"];

    protected override string[] ListImagesCommand => ["images"];

    protected override string[] PullImageCommand => ["pull"];

    protected override string[] RemoveImageCommand => ["rmi"];

    protected override IReadOnlyList<Container> ParseContainersCore(string json) => ParseContainers(json);

    protected override IReadOnlyList<ContainerImage> ParseImagesCore(string json) => ParseImages(json);

    internal static IReadOnlyList<Container> ParseContainers(string json)
    {
        List<ContainerResponse> responses = JsonSerializer.Deserialize<List<ContainerResponse>>(json, JsonOptions) ?? [];
        return responses.Select(MapContainer).ToList();
    }

    internal static IReadOnlyList<ContainerImage> ParseImages(string json)
    {
        List<ImageResponse> responses = JsonSerializer.Deserialize<List<ImageResponse>>(json, JsonOptions) ?? [];
        return responses.Select(MapImage).ToList();
    }

    private static Container MapContainer(ContainerResponse response) => new()
    {
        Id = ShortenId(response.Id),
        Name = response.Name,
        Image = response.Image,
        State = MapState(response.State),
        Ports = FormatPorts(response.Ports),
        CreatedAt = DateTimeOffset.FromUnixTimeSeconds(response.CreatedAt),
        StateChangedAt = DateTimeOffset.FromUnixTimeSeconds(response.StateChangedAt),
    };

    private static ContainerImage MapImage(ImageResponse response) => new()
    {
        Repository = response.Repository,
        Tag = response.Tag,
        Id = ShortenId(StripDigestAlgorithm(response.Id)),
        Created = DateTimeOffset.FromUnixTimeSeconds(response.Created),
        Size = FormatSize(response.Size),
    };

    private static ContainerState MapState(int state) => state switch
    {
        2 => ContainerState.Running,
        3 => ContainerState.Exited,
        _ => ContainerState.Unknown,
    };

    private static string FormatPorts(IReadOnlyList<PortResponse>? ports)
    {
        if (ports is null || ports.Count == 0)
        {
            return EmDash;
        }

        return string.Join('\n', ports.Select(p => $"{p.HostPort}\u2192{p.ContainerPort}/{FormatProtocol(p.Protocol)}"));
    }

    private static string FormatProtocol(int protocol) => protocol switch
    {
        6 => "tcp",
        17 => "udp",
        _ => protocol.ToString(CultureInfo.InvariantCulture),
    };
}
