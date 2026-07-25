using System.Linq;
using System.Text.Json;

namespace Knarr.Service.AppleContainerCli;

/// <summary>
/// <see cref="IContainerCliProvider"/> backed by Apple's first-party <c>container</c> CLI on macOS.
/// Supplies the CLI-specific command verbs and JSON parsing; all process execution and formatting
/// live in <see cref="ContainerCliProviderBase"/>. Every method maps 1:1 onto a single
/// <c>container</c> invocation.
/// </summary>
internal sealed class AppleContainerCliProvider(ILogger<AppleContainerCliProvider> logger)
    : ContainerCliProviderBase(logger)
{
    protected override string Executable => "container";

    protected override string PlatformName => "macOS";

    protected override string[] RemoveContainerCommand => ["delete"];

    protected override string[] ListImagesCommand => ["image", "list"];

    protected override string[] PullImageCommand => ["image", "pull"];

    protected override string[] RemoveImageCommand => ["image", "delete"];

    protected override IReadOnlyList<Container> ParseContainersCore(string json) => ParseContainers(json);

    protected override IReadOnlyList<ContainerImage> ParseImagesCore(string json) => ParseImages(json);

    internal static IReadOnlyList<Container> ParseContainers(string json)
    {
        List<AppleContainerElement> elements =
            JsonSerializer.Deserialize<List<AppleContainerElement>>(json, JsonOptions) ?? [];
        return elements.Select(MapContainer).ToList();
    }

    internal static IReadOnlyList<ContainerImage> ParseImages(string json)
    {
        List<AppleImageElement> elements =
            JsonSerializer.Deserialize<List<AppleImageElement>>(json, JsonOptions) ?? [];
        return elements.Select(MapImage).ToList();
    }

    private static Container MapContainer(AppleContainerElement element) => new()
    {
        // The Apple CLI uses the (user-supplied or generated) id as both id and name; keep it intact.
        Id = element.Id,
        Name = element.Id,
        Image = element.Configuration.Image.Reference,
        State = MapState(element.Status.State),
        Ports = FormatPorts(element.Configuration.PublishedPorts),
        CreatedAt = element.Configuration.CreationDate,
        StateChangedAt = element.Status.StartedDate ?? element.Configuration.CreationDate,
    };

    private static ContainerImage MapImage(AppleImageElement element)
    {
        var (repository, tag) = SplitReference(element.Configuration.Name);
        return new ContainerImage
        {
            Repository = repository,
            Tag = tag,
            Id = ShortenId(StripDigestAlgorithm(element.Id)),
            Created = element.Configuration.CreationDate,
            Size = FormatSize(element.Variants?.Sum(v => v.Size) ?? 0),
        };
    }

    private static ContainerState MapState(string state) => state.ToLowerInvariant() switch
    {
        "running" => ContainerState.Running,
        "stopped" or "exited" => ContainerState.Exited,
        "created" => ContainerState.Created,
        "paused" => ContainerState.Paused,
        _ => ContainerState.Unknown,
    };

    private static string FormatPorts(IReadOnlyList<ApplePublishedPort>? ports)
    {
        if (ports is null || ports.Count == 0)
        {
            return EmDash;
        }

        return string.Join(
            '\n',
            ports.Select(p => $"{p.HostPort}\u2192{p.ContainerPort}/{FormatProtocol(p.Proto)}"));
    }

    private static string FormatProtocol(string? protocol) =>
        string.IsNullOrWhiteSpace(protocol) ? "tcp" : protocol.ToLowerInvariant();

    private static (string Repository, string Tag) SplitReference(string reference)
    {
        var lastColon = reference.LastIndexOf(':');
        var lastSlash = reference.LastIndexOf('/');

        // A colon only denotes a tag when it appears after the final path separator; otherwise it is
        // a registry port (e.g. "localhost:5000/img") and the reference carries no explicit tag.
        return lastColon > lastSlash
            ? (reference[..lastColon], reference[(lastColon + 1)..])
            : (reference, "latest");
    }
}
