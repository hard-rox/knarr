using System;
using System.Collections.Generic;

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

        IReadOnlyList<Container> containers = WslcContainerCliProvider.ParseContainers(json);

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

        ContainerImage image = Assert.Single(WslcContainerCliProvider.ParseImages(json));

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
        Assert.Empty(WslcContainerCliProvider.ParseContainers("[]"));
    }

    [Fact]
    public void ParseImages_EmptyArray_ReturnsEmpty()
    {
        Assert.Empty(WslcContainerCliProvider.ParseImages("[]"));
    }
}
