using System.Linq;
using System.Text.Json;

namespace Knarr.Service.AppleContainerCli;

internal sealed class AppleContainerCliProvider(ILogger<AppleContainerCliProvider> logger)
    : ContainerCliProviderBase(logger)
{
    protected override string Executable => "container";

    protected override string PlatformName => "macOS";

    protected override string[] RemoveContainerCommand => ["delete"];

    protected override string[] ListImagesCommand => ["image", "list"];

    protected override string[] PullImageCommand => ["image", "pull"];

    protected override string[] RemoveImageCommand => ["image", "delete"];

    protected override string[] InspectImageCommand => ["image", "inspect"];

    protected override string[] InspectContainerCommand => ["inspect"];

    protected override string[] RunContainerCommand => ["run"];

    protected override string[] LogsCommand => ["logs"];

    public override bool SupportsPublishAllPorts => false;

    public override bool SupportsLogTimestamps => false;

    public override bool SupportsLogTimeRange => false;

    public override bool SupportsBootLogs => true;

    protected override string TailLinesFlag => "-n";

    protected override IReadOnlyList<Container> ParseContainersCore(string json) => ParseContainers(json);

    protected override IReadOnlyList<ContainerImage> ParseImagesCore(string json) => ParseImages(json);

    protected override ImageInspection ParseImageInspectionCore(string json) => ParseAppleImageInspection(json);

    protected override ContainerInspection ParseContainerInspectionCore(string json) => ParseAppleContainerInspection(json);

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

    // Best-effort: maps identity and config fields from the Apple image descriptor shape; raw JSON always present.
    internal static ImageInspection ParseAppleImageInspection(string json)
    {
        string rawJson = PrettyPrintJson(json);
        try
        {
            AppleImageElement? element =
                (JsonSerializer.Deserialize<List<AppleImageElement>>(json, JsonOptions) ?? []).FirstOrDefault();

            if (element is null)
            {
                return new ImageInspection { Id = string.Empty, RawJson = rawJson };
            }

            long size = element.Variants?.Sum(v => v.Size) ?? 0;

            // Pick the first variant with a known architecture for config fields.
            AppleImageVariant? variant = element.Variants?.FirstOrDefault(
                v => v.Platform?.Architecture is { Length: > 0 } arch && arch != "unknown");
            AppleImageVariantInnerConfig? config = variant?.Config?.Config;

            List<string> repoDigests = element.Configuration.Descriptor?.Digest is { Length: > 0 } d ? [d] : [];

            return new ImageInspection
            {
                Id = element.Id,
                ShortId = ShortenId(StripDigestAlgorithm(element.Id)),
                RepoTags = [element.Configuration.Name],
                RepoDigests = repoDigests,
                Architecture = variant?.Platform?.Architecture ?? string.Empty,
                Os = variant?.Config?.Os ?? string.Empty,
                Created = element.Configuration.CreationDate,
                SizeBytes = size,
                Size = FormatSize(size),
                Entrypoint = config?.Entrypoint?.AsReadOnly() ?? (IReadOnlyList<string>)[],
                Command = config?.Cmd?.AsReadOnly() ?? (IReadOnlyList<string>)[],
                EnvironmentVariables = MapEnvironment(config?.Env),
                StopSignal = config?.StopSignal ?? string.Empty,
                RawJson = rawJson,
            };
        }
        catch (JsonException)
        {
            return new ImageInspection { Id = string.Empty, RawJson = rawJson };
        }
    }

    // Best-effort: macOS container inspect schema differs; always populate RawJson.
    internal static ContainerInspection ParseAppleContainerInspection(string json)
    {
        string rawJson = PrettyPrintJson(json);
        try
        {
            AppleContainerElement? element =
                (JsonSerializer.Deserialize<List<AppleContainerElement>>(json, JsonOptions) ?? []).FirstOrDefault();

            if (element is null)
            {
                return new ContainerInspection { Id = string.Empty, RawJson = rawJson };
            }

            AppleInitProcess? init = element.Configuration.InitProcess;
            AppleContainerResources? resources = element.Configuration.Resources;

            return new ContainerInspection
            {
                Id = element.Id,
                ShortId = element.Id.Length > 12 ? element.Id[..12] : element.Id,
                Name = element.Id,
                Image = element.Configuration.Image.Reference,
                Created = element.Configuration.CreationDate,
                Status = element.Status.State,
                IsRunning = MapState(element.Status.State) == ContainerState.Running,
                StartedAt = element.Status.StartedDate,
                Entrypoint = init?.Executable is { Length: > 0 } exe ? [exe] : [],
                Command = init?.Arguments?.AsReadOnly() ?? (IReadOnlyList<string>)[],
                WorkingDirectory = init?.WorkingDirectory ?? string.Empty,
                EnvironmentVariables = MapEnvironment(init?.Environment),
                Networks = MapStatusNetworks(element.Status.Networks),
                MemoryBytes = resources?.MemoryInBytes ?? 0,
                NanoCpus = resources is not null ? (long)(resources.Cpus * 1_000_000_000) : 0,
                RawJson = rawJson,
            };
        }
        catch (JsonException)
        {
            return new ContainerInspection { Id = string.Empty, RawJson = rawJson };
        }
    }

    private static IReadOnlyList<ContainerNetworkInfo> MapStatusNetworks(List<AppleContainerStatusNetwork>? networks)
    {
        if (networks is null || networks.Count == 0)
        {
            return [];
        }

        return [.. networks.Select(n =>
        {
            string ipAddress = n.Ipv4Address ?? string.Empty;
            int prefixLen = 0;
            int slash = ipAddress.IndexOf('/');
            if (slash >= 0)
            {
                _ = int.TryParse(ipAddress[(slash + 1)..], out prefixLen);
                ipAddress = ipAddress[..slash];
            }

            return new ContainerNetworkInfo(
                Name: n.Network ?? string.Empty,
                Gateway: n.Ipv4Gateway ?? string.Empty,
                IPAddress: ipAddress,
                IPPrefixLen: prefixLen,
                MacAddress: n.MacAddress ?? string.Empty,
                Aliases: n.Hostname is { Length: > 0 } h ? [h] : []);
        })];
    }

    // Splits "KEY=VALUE" on the first '='; entries without '=' get an empty value.
    private static IReadOnlyList<InspectionEntry> MapEnvironment(List<string>? entries)
    {
        if (entries is null || entries.Count == 0)
        {
            return [];
        }

        return [.. entries.Select(e =>
        {
            int sep = e.IndexOf('=');
            return sep < 0
                ? new InspectionEntry(e, string.Empty)
                : new InspectionEntry(e[..sep], e[(sep + 1)..]);
        })];
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
        (string repository, string tag) = SplitReference(element.Configuration.Name);
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
        int lastColon = reference.LastIndexOf(':');
        int lastSlash = reference.LastIndexOf('/');

        // A colon only denotes a tag when it appears after the final path separator; otherwise it is
        // a registry port (e.g. "localhost:5000/img") and the reference carries no explicit tag.
        return lastColon > lastSlash
            ? (reference[..lastColon], reference[(lastColon + 1)..])
            : (reference, "latest");
    }
}
