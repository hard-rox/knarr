using System.Text.Json.Nodes;
using JsonLinq;

namespace Knarr.Service;

/// <summary>
/// <see cref="IContainerCliProvider"/> for Windows, delegating to the WSL container CLI
/// (<c>wslc</c>). Its command surface is Docker-like: top-level lifecycle verbs plus <c>images</c>
/// and <c>rmi</c> for images. List output is requested as JSON.
/// </summary>
internal sealed class WslcCliProvider : CliProviderBase
{
    protected override string Executable => "wslc";

    public override async Task<IReadOnlyList<ContainerSummary>> ListContainersAsync(
        bool includeAll = true,
        CancellationToken cancellationToken = default)
    {
        var arguments = includeAll
            ? new[] { "list", "--all", "--format", "json" }
            : ["list", "--format", "json"];

        var json = await CaptureAsync(cancellationToken, arguments).ConfigureAwait(false);
        return ParseContainers(json);
    }

    public override async Task<IReadOnlyList<ImageSummary>> ListImagesAsync(
        CancellationToken cancellationToken = default)
    {
        var json = await CaptureAsync(cancellationToken, "images", "--format", "json").ConfigureAwait(false);
        return ParseImages(json);
    }

    public override Task RemoveContainerAsync(string id, bool force = false, CancellationToken cancellationToken = default)
        => force
            ? RunAsync(cancellationToken, "remove", "--force", id)
            : RunAsync(cancellationToken, "remove", id);

    public override Task PullImageAsync(string reference, CancellationToken cancellationToken = default)
        => RunAsync(cancellationToken, "pull", reference);

    public override Task RemoveImageAsync(string reference, bool force = false, CancellationToken cancellationToken = default)
        => force
            ? RunAsync(cancellationToken, "rmi", "--force", reference)
            : RunAsync(cancellationToken, "rmi", reference);

    // ----- Parsing -----
    // wslc mirrors Docker's flat JSON rows, so property names follow the Docker column convention.

    internal static IReadOnlyList<ContainerSummary> ParseContainers(string json)
    {
        var containers = new List<ContainerSummary>();
        if (JsonNode.Parse(json) is not JsonArray array)
        {
            return containers;
        }

        foreach (var element in array)
        {
            if (element is null)
            {
                continue;
            }

            var id = element.Value<string>("ID")
                ?? element.Value<string>("Id")
                ?? element.Value<string>("ContainerID")
                ?? string.Empty;
            var name = element.Value<string>("Names")
                ?? element.Value<string>("Name")
                ?? id;
            var image = element.Value<string>("Image") ?? string.Empty;
            var status = element.Value<string>("State")
                ?? element.Value<string>("Status");
            var ports = element.Value<string>("Ports");

            containers.Add(new ContainerSummary
            {
                Id = id,
                Name = name,
                Image = image,
                Status = ParseStatus(status),
                Ports = string.IsNullOrWhiteSpace(ports) ? "\u2014" : ports!,
            });
        }

        return containers;
    }

    internal static IReadOnlyList<ImageSummary> ParseImages(string json)
    {
        var images = new List<ImageSummary>();
        if (JsonNode.Parse(json) is not JsonArray array)
        {
            return images;
        }

        foreach (var element in array)
        {
            if (element is null)
            {
                continue;
            }

            var repository = element.Value<string>("Repository") ?? string.Empty;
            var tag = element.Value<string>("Tag") ?? "latest";
            var id = element.Value<string>("ID")
                ?? element.Value<string>("Id")
                ?? string.Empty;
            var created = element.Value<string>("CreatedSince")
                ?? element.Value<string>("CreatedAt")
                ?? element.Value<string>("Created");
            var size = element.Value<string>("Size");

            // Some builds emit a single "repo:tag" reference rather than split columns.
            if (string.IsNullOrWhiteSpace(repository))
            {
                var reference = element.Value<string>("reference")
                    ?? element.Value<string>("Name");
                if (!string.IsNullOrWhiteSpace(reference))
                {
                    (repository, tag) = SplitReference(reference!);
                }
            }

            images.Add(new ImageSummary
            {
                Repository = repository,
                Tag = tag,
                Id = id,
                Created = string.IsNullOrWhiteSpace(created) ? "\u2014" : created!,
                Size = string.IsNullOrWhiteSpace(size) ? "\u2014" : size!,
            });
        }

        return images;
    }
}
