using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Knarr.App.Models;
using Knarr.App.Services;
using NSubstitute;
using Xunit;

namespace Knarr.App.Tests.Services;

public class ContainerCliProviderTests
{
    private static ICliProcessRunner RunnerReturning(string stdout, int exitCode = 0, string stderr = "")
    {
        var runner = Substitute.For<ICliProcessRunner>();
        runner
            .RunAsync(Arg.Any<string>(), Arg.Any<IReadOnlyList<string>>(), Arg.Any<CancellationToken>())
            .Returns(new CliProcessResult(exitCode, stdout, stderr));
        return runner;
    }

    [Fact]
    public async Task Apple_ListContainers_ParsesInspectStyleJson()
    {
        const string json = """
        [
          { "status": "running", "configuration": { "id": "web", "image": { "reference": "docker.io/library/nginx:latest" } } },
          { "status": "stopped", "configuration": { "id": "db", "image": { "reference": "postgres:16" } } }
        ]
        """;
        var provider = new AppleContainerCliProvider(RunnerReturning(json));

        var containers = await provider.ListContainersAsync();

        Assert.Equal(2, containers.Count);
        Assert.Equal("web", containers[0].Id);
        Assert.Equal("docker.io/library/nginx:latest", containers[0].Image);
        Assert.Equal(ContainerStatus.Running, containers[0].Status);
        Assert.Equal(ContainerStatus.Exited, containers[1].Status);
    }

    [Fact]
    public async Task Apple_ListImages_ParsesReferenceAndSize()
    {
        const string json = """
        [ { "reference": "docker.io/library/nginx:latest", "descriptor": { "digest": "sha256:abc", "size": 1048576 } } ]
        """;
        var provider = new AppleContainerCliProvider(RunnerReturning(json));

        var images = await provider.ListImagesAsync();

        var image = Assert.Single(images);
        Assert.Equal("docker.io/library/nginx", image.Repository);
        Assert.Equal("latest", image.Tag);
        Assert.Equal("abc", image.Id);
        Assert.Equal("1.0 MB", image.Size);
    }

    [Fact]
    public async Task Apple_ListImages_ParsesNestedConfigurationSchema()
    {
        const string json = """
        [
          {
            "configuration": {
              "descriptor": { "digest": "sha256:96498ffd522e", "size": 12212 },
              "name": "docker.io/library/hello-world:latest"
            },
            "id": "96498ffd522e",
            "variants": []
          }
        ]
        """;
        var provider = new AppleContainerCliProvider(RunnerReturning(json));

        var image = Assert.Single(await provider.ListImagesAsync());

        Assert.Equal("docker.io/library/hello-world", image.Repository);
        Assert.Equal("latest", image.Tag);
        Assert.Equal("96498ffd522e", image.Id);
    }

    [Fact]
    public async Task Apple_StartContainer_InvokesExpectedCommand()
    {
        var runner = RunnerReturning(string.Empty);
        var provider = new AppleContainerCliProvider(runner);

        await provider.StartContainerAsync("web");

        await runner.Received().RunAsync(
            "container",
            Arg.Is<IReadOnlyList<string>>(a => a != null && a.SequenceEqual(new[] { "start", "web" })),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Apple_RemoveImage_WithForce_AddsForceFlag()
    {
        var runner = RunnerReturning(string.Empty);
        var provider = new AppleContainerCliProvider(runner);

        await provider.RemoveImageAsync("nginx:latest", force: true);

        await runner.Received().RunAsync(
            "container",
            Arg.Is<IReadOnlyList<string>>(a => a != null && a.SequenceEqual(new[] { "image", "delete", "--force", "nginx:latest" })),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task NonZeroExit_ThrowsCliCommandException()
    {
        var provider = new AppleContainerCliProvider(RunnerReturning(string.Empty, exitCode: 1, stderr: "no such container"));

        var ex = await Assert.ThrowsAsync<CliCommandException>(() => provider.StartContainerAsync("ghost"));

        Assert.Equal(1, ex.ExitCode);
        Assert.Contains("no such container", ex.Message);
    }

    [Fact]
    public async Task Wslc_ListContainers_ParsesDockerStyleJson()
    {
        const string json = """
        [ { "ID": "abc123", "Names": "web", "Image": "nginx:latest", "State": "running", "Ports": "8080->80" } ]
        """;
        var provider = new WslcCliProvider(RunnerReturning(json));

        var container = Assert.Single(await provider.ListContainersAsync());

        Assert.Equal("abc123", container.Id);
        Assert.Equal("web", container.Name);
        Assert.Equal(ContainerStatus.Running, container.Status);
        Assert.Equal("8080->80", container.Ports);
    }

    [Fact]
    public async Task Wslc_ListImages_ParsesColumns()
    {
        const string json = """
        [ { "Repository": "nginx", "Tag": "latest", "ID": "sha256:abc", "CreatedSince": "3 days ago", "Size": "187MB" } ]
        """;
        var provider = new WslcCliProvider(RunnerReturning(json));

        var image = Assert.Single(await provider.ListImagesAsync());

        Assert.Equal("nginx", image.Repository);
        Assert.Equal("latest", image.Tag);
        Assert.Equal("187MB", image.Size);
        Assert.Equal("3 days ago", image.Created);
    }

    [Fact]
    public async Task Wslc_RemoveContainer_UsesRemoveVerb()
    {
        var runner = RunnerReturning(string.Empty);
        var provider = new WslcCliProvider(runner);

        await provider.RemoveContainerAsync("abc123", force: true);

        await runner.Received().RunAsync(
            "wslc",
            Arg.Is<IReadOnlyList<string>>(a => a != null && a.SequenceEqual(new[] { "remove", "--force", "abc123" })),
            Arg.Any<CancellationToken>());
    }
}
