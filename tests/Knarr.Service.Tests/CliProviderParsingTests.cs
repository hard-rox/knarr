namespace Knarr.Service.Tests;

/// <summary>
/// Unit tests for the container CLI providers' JSON parsing. Process execution is delegated to
/// CliWrap and is not exercised here; these tests cover the platform-specific parsing logic via
/// the providers' internal static parse methods (exposed through InternalsVisibleTo).
/// </summary>
public class CliProviderParsingTests
{
    [Fact]
    public void Apple_ParseContainers_ReadsInspectStyleJson()
    {
        const string json = """
        [
          { "status": "running", "configuration": { "id": "web", "image": { "reference": "docker.io/library/nginx:latest" } } },
          { "status": "stopped", "configuration": { "id": "db", "image": { "reference": "postgres:16" } } }
        ]
        """;

        var containers = AppleContainerCliProvider.ParseContainers(json);

        Assert.Equal(2, containers.Count);
        Assert.Equal("web", containers[0].Id);
        Assert.Equal("docker.io/library/nginx:latest", containers[0].Image);
        Assert.Equal(ContainerStatus.Running, containers[0].Status);
        Assert.Equal(ContainerStatus.Exited, containers[1].Status);
    }

    [Fact]
    public void Apple_ParseImages_ReadsReferenceAndSize()
    {
        const string json = """
        [ { "reference": "docker.io/library/nginx:latest", "descriptor": { "digest": "sha256:abc", "size": 1048576 } } ]
        """;

        var images = AppleContainerCliProvider.ParseImages(json);

        var image = Assert.Single(images);
        Assert.Equal("docker.io/library/nginx", image.Repository);
        Assert.Equal("latest", image.Tag);
        Assert.Equal("abc", image.Id);
        Assert.Equal("1.0 MB", image.Size);
    }

    [Fact]
    public void Apple_ParseImages_ReadsNestedConfigurationSchema()
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

        var image = Assert.Single(AppleContainerCliProvider.ParseImages(json));

        Assert.Equal("docker.io/library/hello-world", image.Repository);
        Assert.Equal("latest", image.Tag);
        Assert.Equal("96498ffd522e", image.Id);
    }

    [Fact]
    public void Apple_ParseContainers_EmptyOrNonArray_ReturnsEmpty()
    {
        Assert.Empty(AppleContainerCliProvider.ParseContainers("[]"));
        Assert.Empty(AppleContainerCliProvider.ParseContainers("{}"));
    }

    [Fact]
    public void Wslc_ParseContainers_ReadsDockerStyleJson()
    {
        const string json = """
        [ { "ID": "abc123", "Names": "web", "Image": "nginx:latest", "State": "running", "Ports": "8080->80" } ]
        """;

        var container = Assert.Single(WslcCliProvider.ParseContainers(json));

        Assert.Equal("abc123", container.Id);
        Assert.Equal("web", container.Name);
        Assert.Equal(ContainerStatus.Running, container.Status);
        Assert.Equal("8080->80", container.Ports);
    }

    [Fact]
    public void Wslc_ParseImages_ReadsColumns()
    {
        const string json = """
        [ { "Repository": "nginx", "Tag": "latest", "ID": "sha256:abc", "CreatedSince": "3 days ago", "Size": "187MB" } ]
        """;

        var image = Assert.Single(WslcCliProvider.ParseImages(json));

        Assert.Equal("nginx", image.Repository);
        Assert.Equal("latest", image.Tag);
        Assert.Equal("187MB", image.Size);
        Assert.Equal("3 days ago", image.Created);
    }

    [Fact]
    public void Wslc_ParseImages_FallsBackToSingleReference()
    {
        const string json = """
        [ { "Name": "docker.io/library/redis:7-alpine", "ID": "sha256:def" } ]
        """;

        var image = Assert.Single(WslcCliProvider.ParseImages(json));

        Assert.Equal("docker.io/library/redis", image.Repository);
        Assert.Equal("7-alpine", image.Tag);
    }
}
