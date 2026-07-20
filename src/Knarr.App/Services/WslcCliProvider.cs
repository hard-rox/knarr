using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Knarr.App.Models;

namespace Knarr.App.Services;

/// <summary>
/// <see cref="IContainerCliProvider"/> for Windows, delegating to the WSL container CLI
/// (<c>wslc</c>). Its command surface is Docker-like: top-level lifecycle verbs plus <c>images</c>
/// and <c>rmi</c> for images. List output is requested as JSON.
/// </summary>
public sealed class WslcCliProvider : CliProviderBase
{
    public WslcCliProvider(ICliProcessRunner runner)
        : base(runner)
    {
    }

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

    public override Task PushImageAsync(string reference, CancellationToken cancellationToken = default)
        => RunAsync(cancellationToken, "push", reference);

    public override Task TagImageAsync(string source, string target, CancellationToken cancellationToken = default)
        => RunAsync(cancellationToken, "tag", source, target);

    public override Task RemoveImageAsync(string reference, bool force = false, CancellationToken cancellationToken = default)
        => force
            ? RunAsync(cancellationToken, "rmi", "--force", reference)
            : RunAsync(cancellationToken, "rmi", reference);

    public override Task PruneImagesAsync(CancellationToken cancellationToken = default)
        => RunAsync(cancellationToken, "image", "prune");

    public override Task<string> InspectImageAsync(string reference, CancellationToken cancellationToken = default)
        => CaptureAsync(cancellationToken, "image", "inspect", reference);

    // ----- Parsing -----
    // wslc mirrors Docker's flat JSON rows, so property names follow the Docker column convention.

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
            var id = element.StringAny("ID", "Id", "ContainerID") ?? string.Empty;
            var name = element.StringAny("Names", "Name") ?? id;
            var image = element.StringAny("Image") ?? string.Empty;
            var status = element.StringAny("State", "Status");
            var ports = element.StringAny("Ports");

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
            var repository = element.StringAny("Repository") ?? string.Empty;
            var tag = element.StringAny("Tag") ?? "latest";
            var id = element.StringAny("ID", "Id") ?? string.Empty;
            var created = element.StringAny("CreatedSince", "CreatedAt", "Created");
            var size = element.StringAny("Size");

            // Some builds emit a single "repo:tag" reference rather than split columns.
            if (string.IsNullOrWhiteSpace(repository))
            {
                var reference = element.StringAny("reference", "Name");
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
