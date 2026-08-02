using System.Globalization;
using System.Linq;
using System.Text.Json;
using Knarr.Service.Exceptions;

namespace Knarr.Service.WslcCli;

internal sealed class WslcCliProvider(ILogger<WslcCliProvider> logger)
    : ContainerCliProviderBase(logger)
{
    protected override string Executable => "wslc";

    protected override string PlatformName => "Windows";

    protected override string[] RemoveContainerCommand => ["remove"];

    protected override string[] ListImagesCommand => ["images"];

    protected override string[] PullImageCommand => ["pull"];

    protected override string[] RemoveImageCommand => ["rmi"];

    protected override string[] InspectImageCommand => ["image", "inspect"];

    protected override string[] InspectContainerCommand => ["container", "inspect"];

    protected override string[] RunContainerCommand => ["run"];

    protected override string[] LogsCommand => ["logs"];

    protected override IReadOnlyList<Container> ParseContainersCore(string json) => ParseContainers(json);

    protected override IReadOnlyList<ContainerImage> ParseImagesCore(string json) => ParseImages(json);

    protected override ImageInspection ParseImageInspectionCore(string json) => ParseImageInspection(json);

    protected override ContainerInspection ParseContainerInspectionCore(string json) => ParseContainerInspection(json);

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

    internal static ImageInspection ParseImageInspection(string json)
    {
        List<ImageInspectResponse> responses =
            JsonSerializer.Deserialize<List<ImageInspectResponse>>(json, JsonOptions) ?? [];

        if (responses.Count == 0)
        {
            throw new EmptyCliResultException("image inspect");
        }

        return MapImageInspection(responses[0], PrettyPrintJson(json));
    }

    internal static ContainerInspection ParseContainerInspection(string json)
    {
        List<ContainerInspectResponse> responses =
            JsonSerializer.Deserialize<List<ContainerInspectResponse>>(json, JsonOptions) ?? [];

        if (responses.Count == 0)
        {
            throw new EmptyCliResultException("container inspect");
        }

        return MapContainerInspection(responses[0], PrettyPrintJson(json));
    }

    private static ImageInspection MapImageInspection(ImageInspectResponse r, string rawJson)
    {
        ImageInspectConfigResponse config = r.Config ?? new(null, null, null, null, null, null, null, null, null);
        string id = r.Id ?? string.Empty;

        return new ImageInspection
        {
            Id = id,
            ShortId = ShortenId(StripDigestAlgorithm(id)),
            RepoTags = r.RepoTags ?? [],
            RepoDigests = r.RepoDigests ?? [],
            Architecture = r.Architecture ?? string.Empty,
            Os = r.Os ?? string.Empty,
            SizeBytes = r.Size,
            Size = FormatSize(r.Size),
            Created = r.Created,
            Author = r.Author ?? string.Empty,
            Comment = r.Comment ?? string.Empty,
            Parent = r.Parent ?? string.Empty,
            Entrypoint = config.Entrypoint ?? [],
            Command = config.Cmd ?? [],
            WorkingDirectory = config.WorkingDir ?? string.Empty,
            User = config.User ?? string.Empty,
            StopSignal = config.StopSignal ?? string.Empty,
            EnvironmentVariables = MapEnvironment(config.Env),
            Labels = MapLabels(config.Labels),
            ExposedPorts = SortExposedPorts(config.ExposedPorts?.Keys),
            Volumes = config.Volumes is null ? [] : [.. config.Volumes.Keys.Order(StringComparer.Ordinal)],
            RawJson = rawJson,
        };
    }

    private static ContainerInspection MapContainerInspection(ContainerInspectResponse r, string rawJson)
    {
        ContainerInspectConfigResponse config = r.Config ?? new(null, null, null, null, null);
        ContainerHostConfigResponse host = r.HostConfig ?? new(0, 0, null);
        ContainerInspectStateResponse state = r.State ?? new(0, null, false, null, null);
        string id = r.Id ?? string.Empty;

        return new ContainerInspection
        {
            Id = id,
            ShortId = ShortenId(id),
            Name = r.Name?.TrimStart('/') ?? string.Empty,
            Image = r.Image ?? string.Empty,
            Created = r.Created,
            Status = state.Status ?? string.Empty,
            IsRunning = state.Running,
            ExitCode = state.ExitCode,
            StartedAt = state.StartedAt,
            FinishedAt = state.FinishedAt,
            Entrypoint = config.Entrypoint ?? [],
            Command = config.Cmd ?? [],
            WorkingDirectory = config.WorkingDir ?? string.Empty,
            User = config.User ?? string.Empty,
            EnvironmentVariables = MapEnvironment(config.Env),
            Labels = MapLabels(r.Labels),
            Ports = MapPortBindings(r.Ports),
            Networks = MapNetworks(r.NetworkSettings?.Networks),
            NetworkMode = host.NetworkMode ?? string.Empty,
            MemoryBytes = host.Memory,
            NanoCpus = host.NanoCpus,
            RawJson = rawJson,
        };
    }

    // Splits "KEY=VALUE" on the first '='; entries without '=' get an empty value.
    private static IReadOnlyList<InspectionEntry> MapEnvironment(IReadOnlyList<string>? entries)
    {
        if (entries is null)
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

    private static IReadOnlyList<InspectionEntry> MapLabels(IReadOnlyDictionary<string, string>? labels)
        => labels is null
            ? []
            : [.. labels.OrderBy(l => l.Key, StringComparer.Ordinal)
                .Select(l => new InspectionEntry(l.Key, l.Value))];

    // The CLI emits port keys lexicographically ("15671" before "4369"); re-sort numerically.
    private static IReadOnlyList<string> SortExposedPorts(IEnumerable<string>? ports)
        => ports is null
            ? []
            : [.. ports
                .OrderBy(p => int.TryParse(p.Split('/')[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out int n)
                    ? n : int.MaxValue)
                .ThenBy(p => p, StringComparer.Ordinal)];

    private static IReadOnlyList<string> MapPortBindings(
        Dictionary<string, List<ContainerPortBindingResponse>>? ports)
    {
        if (ports is null || ports.Count == 0)
        {
            return [];
        }

        return [.. ports.Select(kvp =>
        {
            List<ContainerPortBindingResponse>? bindings = kvp.Value;
            if (bindings is null || bindings.Count == 0)
            {
                return $"{kvp.Key} (unbound)";
            }

            ContainerPortBindingResponse b = bindings[0];
            return string.IsNullOrEmpty(b.HostIp)
                ? $"{kvp.Key} \u2192 :{b.HostPort}"
                : $"{kvp.Key} \u2192 {b.HostIp}:{b.HostPort}";
        })];
    }

    private static IReadOnlyList<ContainerNetworkInfo> MapNetworks(
        Dictionary<string, ContainerNetworkEntryResponse>? networks)
    {
        if (networks is null || networks.Count == 0)
        {
            return [];
        }

        return [.. networks.Select(kvp => new ContainerNetworkInfo(
            Name: kvp.Key,
            Gateway: kvp.Value.Gateway ?? string.Empty,
            IPAddress: kvp.Value.IPAddress ?? string.Empty,
            IPPrefixLen: kvp.Value.IPPrefixLen,
            MacAddress: kvp.Value.MacAddress ?? string.Empty,
            Aliases: kvp.Value.Aliases ?? []))];
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
