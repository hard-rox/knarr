using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Knarr.App.Models;

namespace Knarr.App.Services;

/// <summary>
/// <see cref="IContainerCliProvider"/> for macOS, delegating to Apple Container's <c>container</c>
/// CLI. Container verbs live at the top level (<c>container start</c>) while image verbs are
/// grouped under <c>container image</c>. List output is requested as JSON.
/// </summary>
public sealed class AppleContainerCliProvider : CliProviderBase
{
    public AppleContainerCliProvider(ICliProcessRunner runner)
        : base(runner)
    {
    }

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

    public override Task PushImageAsync(string reference, CancellationToken cancellationToken = default)
        => RunAsync(cancellationToken, "image", "push", reference);

    public override Task TagImageAsync(string source, string target, CancellationToken cancellationToken = default)
        => RunAsync(cancellationToken, "image", "tag", source, target);

    public override Task RemoveImageAsync(string reference, bool force = false, CancellationToken cancellationToken = default)
        => force
            ? RunAsync(cancellationToken, "image", "delete", "--force", reference)
            : RunAsync(cancellationToken, "image", "delete", reference);

    public override Task PruneImagesAsync(CancellationToken cancellationToken = default)
        => RunAsync(cancellationToken, "image", "prune");

    public override Task<string> InspectImageAsync(string reference, CancellationToken cancellationToken = default)
        => CaptureAsync(cancellationToken, "image", "inspect", reference);

    // ----- Parsing -----
    // Apple Container's list JSON is inspect-style: an array of objects that nest the container
    // configuration. Navigation is defensive so schema drift degrades gracefully rather than throws.

    private static IReadOnlyList<ContainerSummary> ParseContainers(string json)
    {
        var containers = new List<ContainerSummary>();
        using var document = JsonDocument.Parse(json);
        if (document.RootElement.ValueKind != JsonValueKind.Array)
        {
            return containers;
        }

        foreach (var element in document.RootElement.EnumerateArray())
        {
            var configuration = element.Property("configuration") ?? element;

            var id = configuration.StringAny("id", "hostname")
                ?? element.StringAny("id")
                ?? string.Empty;

            var imageElement = configuration.PropertyAny("image");
            var image = imageElement?.StringAny("reference", "name")
                ?? configuration.StringAny("image")
                ?? element.StringAny("image")
                ?? string.Empty;

            var status = element.StringAny("status", "state")
                ?? configuration.StringAny("status", "state");

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

    private static IReadOnlyList<ImageSummary> ParseImages(string json)
    {
        var images = new List<ImageSummary>();
        using var document = JsonDocument.Parse(json);
        if (document.RootElement.ValueKind != JsonValueKind.Array)
        {
            return images;
        }

        foreach (var element in document.RootElement.EnumerateArray())
        {
            // Apple's image list nests the reference/descriptor under "configuration"; fall back to
            // the element itself for older/flatter shapes.
            var configuration = element.Property("configuration") ?? element;

            var reference = configuration.StringAny("name", "reference")
                ?? element.StringAny("reference", "name")
                ?? string.Empty;
            var (repository, tag) = SplitReference(reference);

            var descriptor = configuration.PropertyAny("descriptor") ?? element.PropertyAny("descriptor");
            var id = element.StringAny("id")
                ?? descriptor?.StringAny("digest")
                ?? string.Empty;
            // Strip the algorithm prefix (e.g. "sha256:") so the ID matches the CLI's DIGEST column.
            var colonIndex = id.IndexOf(':');
            if (colonIndex >= 0)
            {
                id = id[(colonIndex + 1)..];
            }

            var size = descriptor?.PropertyAny("size");
            var sizeText = size is { ValueKind: JsonValueKind.Number }
                ? FormatSize(size.Value.GetInt64())
                : "\u2014";

            images.Add(new ImageSummary
            {
                Repository = repository,
                Tag = tag,
                Id = id,
                Size = sizeText,
            });
        }

        return images;
    }
}
