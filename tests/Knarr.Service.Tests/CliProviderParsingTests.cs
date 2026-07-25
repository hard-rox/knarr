using System;
using System.Collections.Generic;
using Knarr.Service.Exceptions;

namespace Knarr.Service.Tests;

/// <summary>
/// Unit tests for the wslc provider's JSON parsing and shaping. Process execution is delegated to
/// CliWrap and is not exercised here; these tests cover the parsing/shaping via the provider's
/// internal static parse methods (exposed through InternalsVisibleTo).
/// </summary>
public class CliProviderParsingTests
{
    [Fact]
    public void ParseContainers_ShapesIdStateAndPorts()
    {
        const string json = """
        [
          {
            "CreatedAt": 1783247887,
            "Id": "54b33d1d705e20329034dc366442f97fb826d2b6952260a152c13666aff5dfb9",
            "Image": "redis",
            "Name": "redis",
            "Ports": [ { "BindingAddress": "127.0.0.1", "ContainerPort": 6379, "HostPort": 6379, "Protocol": 6 } ],
            "State": 2,
            "StateChangedAt": 1784609346
          },
          {
            "CreatedAt": 1783247931,
            "Id": "ecdf3a98ed66d0f169b1227fc8737bc61e12085aae71ed4fdcede0117f094b6d",
            "Image": "rabbitmq:management",
            "Name": "rabbitmq",
            "Ports": [],
            "State": 3,
            "StateChangedAt": 1784628212
          }
        ]
        """;

        IReadOnlyList<Container> containers = WslcCli.WslcCliProvider.ParseContainers(json);

        Assert.Equal(2, containers.Count);

        Container redis = containers[0];
        Assert.Equal("54b33d1d705e", redis.Id);
        Assert.Equal(12, redis.Id.Length);
        Assert.Equal("redis", redis.Name);
        Assert.Equal(ContainerState.Running, redis.State);
        Assert.Equal("6379\u21926379/tcp", redis.Ports);
        Assert.Equal(DateTimeOffset.FromUnixTimeSeconds(1783247887), redis.CreatedAt);

        Container rabbit = containers[1];
        Assert.Equal(ContainerState.Exited, rabbit.State);
        Assert.Equal("\u2014", rabbit.Ports);
    }

    [Fact]
    public void ParseImages_ShapesIdAndSize()
    {
        const string json = """
        [
          {
            "Created": 1782970335,
            "Id": "sha256:a2334b0057861fbfdf227b60817c5a45a9233719ce92e912e0cdb60c08203f5b",
            "Repository": "rabbitmq",
            "Size": 250460256,
            "Tag": "management"
          }
        ]
        """;

        ContainerImage image = Assert.Single(WslcCli.WslcCliProvider.ParseImages(json));

        Assert.Equal("rabbitmq", image.Repository);
        Assert.Equal("management", image.Tag);
        Assert.Equal("a2334b005786", image.Id);
        Assert.Equal(12, image.Id.Length);
        Assert.Equal("239 MB", image.Size);
        Assert.Equal(DateTimeOffset.FromUnixTimeSeconds(1782970335), image.Created);
    }

    [Fact]
    public void ParseContainers_EmptyArray_ReturnsEmpty()
    {
        Assert.Empty(WslcCli.WslcCliProvider.ParseContainers("[]"));
    }

    [Fact]
    public void ParseImages_EmptyArray_ReturnsEmpty()
    {
        Assert.Empty(WslcCli.WslcCliProvider.ParseImages("[]"));
    }
}

/// <summary>
/// Unit tests for <see cref="AggregateCliCommandException"/>, which aggregates the per-item failures of a
/// bulk operation that loops one CLI invocation per item.
/// </summary>
public class AggregateCliCommandExceptionTests
{
    [Fact]
    public void Message_ForSingleFailure_MatchesUnderlyingCommand()
    {
        CliCommandException inner = new("container start abc", 1, "boom");
        AggregateCliCommandException ex = new([inner]);

        Assert.Single(ex.Failures);
        Assert.Equal(inner.Message, ex.Message);
    }

    [Fact]
    public void Message_ForMultipleFailures_ListsEveryFailure()
    {
        CliCommandException first = new("container start abc", 1, "boom");
        CliCommandException second = new("container start def", 1, "kaboom");

        AggregateCliCommandException ex = new([first, second]);

        Assert.Equal(2, ex.Failures.Count);
        Assert.Contains("2 commands failed", ex.Message);
        Assert.Contains("boom", ex.Message);
        Assert.Contains("kaboom", ex.Message);
    }
}

/// <summary>
/// Unit tests for the Apple <c>container</c> provider's JSON parsing and shaping. The sample
/// payloads match the real <c>container list --format json</c> / <c>container image list --format json</c>
/// output structure (nested configuration/status, ISO 8601 dates).
/// </summary>
public class AppleCliProviderParsingTests
{
    [Fact]
    public void ParseContainers_ShapesIdImageStateAndDates()
    {
        const string json = """
        [{"configuration":{"capAdd":[],"capDrop":[],"creationDate":"2026-07-22T01:29:45Z","dns":{"nameservers":[],"options":[],"searchDomains":[]},"id":"dcf0335b-14f3-4afa-b5f0-dce3f3462c52","image":{"descriptor":{"digest":"sha256:c3cbe1cc1aa588a64951ac6286e0df7b27fe2e6324b1001c619bb358770c0178","mediaType":"application/vnd.oci.image.index.v1+json","size":12212},"reference":"docker.io/library/hello-world:latest"},"initProcess":{"arguments":[],"environment":["PATH=/usr/local/sbin:/usr/local/bin:/usr/sbin:/usr/bin:/sbin:/bin"],"executable":"/hello","rlimits":[],"supplementalGroups":[],"terminal":false,"user":{"id":{"gid":0,"uid":0}},"workingDirectory":"/"},"labels":{},"mounts":[],"networks":[{"network":"default","options":{"hostname":"dcf0335b-14f3-4afa-b5f0-dce3f3462c52","mtu":1280}}],"platform":{"architecture":"arm64","os":"linux"},"publishedPorts":[],"publishedSockets":[],"readOnly":false,"resources":{"cpuOverhead":1,"cpus":4,"memoryInBytes":1073741824},"rosetta":false,"runtimeHandler":"container-runtime-linux","ssh":false,"sysctls":{},"useInit":false,"virtualization":false},"id":"dcf0335b-14f3-4afa-b5f0-dce3f3462c52","status":{"networks":[],"startedDate":"2026-07-22T01:29:46Z","state":"stopped"}}]
        """;

        Container container = Assert.Single(AppleContainerCli.AppleContainerCliProvider.ParseContainers(json));

        Assert.Equal("dcf0335b-14f3-4afa-b5f0-dce3f3462c52", container.Id);
        Assert.Equal("dcf0335b-14f3-4afa-b5f0-dce3f3462c52", container.Name);
        Assert.Equal("docker.io/library/hello-world:latest", container.Image);
        Assert.Equal(ContainerState.Exited, container.State);
        Assert.Equal("\u2014", container.Ports);
        Assert.Equal(new DateTimeOffset(2026, 7, 22, 1, 29, 45, TimeSpan.Zero), container.CreatedAt);
        Assert.Equal(new DateTimeOffset(2026, 7, 22, 1, 29, 46, TimeSpan.Zero), container.StateChangedAt);
    }

    [Fact]
    public void ParseImages_ShapesRepositoryTagIdAndSize()
    {
        const string json = """
        [{"configuration":{"creationDate":"2026-03-23T21:34:00Z","descriptor":{"digest":"sha256:c3cbe1cc1aa588a64951ac6286e0df7b27fe2e6324b1001c619bb358770c0178","mediaType":"application/vnd.oci.image.index.v1+json","size":12212},"name":"docker.io/library/hello-world:latest"},"id":"c3cbe1cc1aa588a64951ac6286e0df7b27fe2e6324b1001c619bb358770c0178","variants":[{"digest":"sha256:5099b89d7666cc2186cad769ddc262ddc7c335b33f5fe79f9ffe50a01282b23e","platform":{"architecture":"arm64","os":"linux","variant":"v8"},"size":4768}]},{"configuration":{"creationDate":"1970-01-01T00:00:00Z","descriptor":{"digest":"sha256:7d9231be1863c289ba522363b5069a5c073c62f34eb240797b0fa289c09cc952","mediaType":"application/vnd.oci.image.index.v1+json","size":306},"name":"ghcr.io/apple/containerization/vminit:0.33.3"},"id":"7d9231be1863c289ba522363b5069a5c073c62f34eb240797b0fa289c09cc952","variants":[{"digest":"sha256:43113e6a2b8a1a99ec9bf97e919e44621590d4c6ed0f862cf30d2fe17d663338","platform":{"architecture":"arm64","os":"linux","variant":"v8"},"size":66840050}]}]
        """;

        IReadOnlyList<ContainerImage> images = AppleContainerCli.AppleContainerCliProvider.ParseImages(json);

        Assert.Equal(2, images.Count);

        ContainerImage hello = images[0];
        Assert.Equal("docker.io/library/hello-world", hello.Repository);
        Assert.Equal("latest", hello.Tag);
        Assert.Equal("c3cbe1cc1aa5", hello.Id);
        Assert.Equal(12, hello.Id.Length);
        Assert.Equal("4.7 KB", hello.Size);
        Assert.Equal(new DateTimeOffset(2026, 3, 23, 21, 34, 0, TimeSpan.Zero), hello.Created);

        ContainerImage vminit = images[1];
        Assert.Equal("ghcr.io/apple/containerization/vminit", vminit.Repository);
        Assert.Equal("0.33.3", vminit.Tag);
        Assert.Equal("7d9231be1863", vminit.Id);
        Assert.Equal("63.7 MB", vminit.Size);
    }

    [Fact]
    public void ParseContainers_EmptyArray_ReturnsEmpty()
    {
        Assert.Empty(AppleContainerCli.AppleContainerCliProvider.ParseContainers("[]"));
    }

    [Fact]
    public void ParseImages_EmptyArray_ReturnsEmpty()
    {
        Assert.Empty(AppleContainerCli.AppleContainerCliProvider.ParseImages("[]"));
    }
}
