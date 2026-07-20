using System.Text.Json.Nodes;
using JsonLinq;

namespace Knarr.Service;

/// <summary>
/// <see cref="IContainerCliProvider"/> for macOS, delegating to Apple Container's <c>container</c>
/// CLI. Container verbs live at the top level (<c>container start</c>) while image verbs are
/// grouped under <c>container image</c>. List output is requested as JSON.
/// </summary>
internal sealed class AppleContainerCliProvider : CliProviderBase
{
    protected override string Executable => "container";

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
        var json = await CaptureAsync(cancellationToken, "image", "list", "--format", "json").ConfigureAwait(false);
        return ParseImages(json);
    }

    public override Task RemoveContainerAsync(string id, bool force = false, CancellationToken cancellationToken = default)
        => force
            ? RunAsync(cancellationToken, "delete", "--force", id)
            : RunAsync(cancellationToken, "delete", id);

    public override Task PullImageAsync(string reference, CancellationToken cancellationToken = default)
        => RunAsync(cancellationToken, "image", "pull", reference);

    public override Task RemoveImageAsync(string reference, bool force = false, CancellationToken cancellationToken = default)
        => force
            ? RunAsync(cancellationToken, "image", "delete", "--force", reference)
            : RunAsync(cancellationToken, "image", "delete", reference);

    // ----- Parsing -----
    // Apple Container's list JSON is inspect-style: an array of objects that nest the container
    // configuration. Navigation is defensive so schema drift degrades gracefully rather than throws.

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

            var configuration = element["configuration"] ?? element;

            var id = configuration.Value<string>("id")
                ?? configuration.Value<string>("hostname")
                ?? element.Value<string>("id")
                ?? string.Empty;

            var image = configuration.ValueAt<string>("image.reference")
                ?? configuration.ValueAt<string>("image.name")
                ?? configuration.Value<string>("image")
                ?? element.Value<string>("image")
                ?? string.Empty;

            var status = element.Value<string>("status")
                ?? element.Value<string>("state")
                ?? configuration.Value<string>("status")
                ?? configuration.Value<string>("state");

            containers.Add(new ContainerSummary
            {
                Id = id,
                Name = id,
                Image = image,
                Status = ParseStatus(status),
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

            // Apple's image list nests the reference/descriptor under "configuration"; fall back to
            // the element itself for older/flatter shapes.
            var configuration = element["configuration"] ?? element;

            var reference = configuration.Value<string>("name")
                ?? configuration.Value<string>("reference")
                ?? element.Value<string>("reference")
                ?? element.Value<string>("name")
                ?? string.Empty;
            var (repository, tag) = SplitReference(reference);

            var id = element.Value<string>("id")
                ?? configuration.ValueAt<string>("descriptor.digest")
                ?? element.ValueAt<string>("descriptor.digest")
                ?? string.Empty;
            // Strip the algorithm prefix (e.g. "sha256:") so the ID matches the CLI's DIGEST column.
            var colonIndex = id.IndexOf(':');
            if (colonIndex >= 0)
            {
                id = id[(colonIndex + 1)..];
            }

            var sizeBytes = configuration.ValueAt<long>("descriptor.size");
            if (sizeBytes == 0)
            {
                sizeBytes = element.ValueAt<long>("descriptor.size");
            }

            images.Add(new ImageSummary
            {
                Repository = repository,
                Tag = tag,
                Id = id,
                Size = FormatSize(sizeBytes),
            });
        }

        return images;
    }
}
