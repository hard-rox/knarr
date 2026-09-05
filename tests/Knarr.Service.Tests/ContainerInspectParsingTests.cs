using System;
using Knarr.Service.Exceptions;
using Knarr.Service.WslcCli;

namespace Knarr.Service.Tests;

/// <summary>
/// Verifies that the wslc <c>container inspect</c> payload is shaped into <see cref="ContainerInspection"/>.
/// </summary>
public class ContainerInspectParsingTests
{
    private const string RunningJson = """
    [{
        "Config": {
            "Cmd": ["100", "5000"],
            "Entrypoint": ["/entrypoint.sh"],
            "Env": ["PATH=/usr/local/sbin:/usr/local/bin:/usr/sbin:/usr/bin:/sbin:/bin"],
            "User": "",
            "WorkingDir": ""
        },
        "Created": "2026-07-28T11:33:30.819788508Z",
        "HostConfig": { "Memory": 0, "NanoCpus": 0, "NetworkMode": "bridge" },
        "Id": "c270199300c65415cde5e0ac8db14b6c4e2da7213e984de2084cbae608034a69",
        "Image": "chentex/random-logger:latest",
        "Labels": {
            "org.opencontainers.image.title": "random-logger",
            "org.opencontainers.image.version": "v1.0.1"
        },
        "Mounts": [],
        "Name": "whispering_rockies",
        "NetworkSettings": {
            "Networks": {
                "bridge": {
                    "Aliases": [],
                    "Gateway": "172.17.0.1",
                    "IPAddress": "172.17.0.2",
                    "IPPrefixLen": 16,
                    "MacAddress": "02:42:ac:11:00:02"
                }
            }
        },
        "Ports": {},
        "State": {
            "ExitCode": 0,
            "FinishedAt": "2026-07-30T06:07:08Z",
            "Running": true,
            "StartedAt": "2026-08-02T10:25:56Z",
            "Status": "running"
        }
    }]
    """;

    private const string StoppedJson = """
    [{
        "Config": {
            "Cmd": ["redis-server"],
            "Entrypoint": ["docker-entrypoint.sh"],
            "Env": ["PATH=/usr/local/sbin:/usr/local/bin:/usr/sbin:/usr/bin:/sbin:/bin", "REDIS_VERSION=8.8.1"],
            "User": "",
            "WorkingDir": "/data"
        },
        "Created": "2026-07-28T05:13:10.204985571Z",
        "HostConfig": { "Memory": 0, "NanoCpus": 0, "NetworkMode": "bridge" },
        "Id": "22f7142ddbb57dddcc3794b91c50d144744c7ed9b4f65e7532bbcd63c620316d",
        "Image": "redis:latest",
        "Labels": {},
        "Mounts": [],
        "Name": "redis",
        "NetworkSettings": {
            "Networks": {
                "bridge": {
                    "Gateway": "172.17.0.1",
                    "IPAddress": "172.17.0.4",
                    "IPPrefixLen": 16,
                    "MacAddress": "02:42:ac:11:00:04"
                }
            }
        },
        "Ports": {
            "6379/tcp": [{ "HostIp": "127.0.0.1", "HostPort": "6379" }]
        },
        "State": {
            "ExitCode": 255,
            "FinishedAt": "2026-07-30T04:32:36Z",
            "Running": false,
            "StartedAt": "2026-07-29T05:11:56Z",
            "Status": "exited"
        }
    }]
    """;

    [Fact]
    public void Parse_RunningContainer_ShapesIdentityAndState()
    {
        ContainerInspection i = WslcCliProvider.ParseContainerInspection(RunningJson);

        Assert.Equal("c270199300c6", i.ShortId);
        Assert.Equal("whispering_rockies", i.Name);
        Assert.Equal("chentex/random-logger:latest", i.Image);
        Assert.True(i.IsRunning);
        Assert.Equal("running", i.Status);
        Assert.Equal(0, i.ExitCode);
    }

    [Fact]
    public void Parse_RunningContainer_MapsNetworkAndLabels()
    {
        ContainerInspection i = WslcCliProvider.ParseContainerInspection(RunningJson);

        Assert.Single(i.Networks);
        Assert.Equal("bridge", i.Networks[0].Name);
        Assert.Equal("172.17.0.1", i.Networks[0].Gateway);
        Assert.Equal("172.17.0.2", i.Networks[0].IPAddress);
        Assert.Equal(16, i.Networks[0].IPPrefixLen);
        Assert.Equal(2, i.Labels.Count);
        Assert.Empty(i.Ports);
    }

    [Fact]
    public void Parse_StoppedContainer_ShapesStateAndPorts()
    {
        ContainerInspection i = WslcCliProvider.ParseContainerInspection(StoppedJson);

        Assert.False(i.IsRunning);
        Assert.Equal("exited", i.Status);
        Assert.Equal(255, i.ExitCode);
        Assert.Single(i.Ports);
        Assert.Equal("6379/tcp \u2192 127.0.0.1:6379", i.Ports[0]);
    }

    [Fact]
    public void Parse_StoppedContainer_SplitsEnvOnFirstEquals()
    {
        ContainerInspection i = WslcCliProvider.ParseContainerInspection(StoppedJson);

        Assert.Equal(2, i.EnvironmentVariables.Count);
        Assert.Equal("REDIS_VERSION", i.EnvironmentVariables[1].Key);
        Assert.Equal("8.8.1", i.EnvironmentVariables[1].Value);
    }

    [Fact]
    public void Parse_EmptyLabels_BecomeEmpty()
    {
        ContainerInspection i = WslcCliProvider.ParseContainerInspection(StoppedJson);

        Assert.Empty(i.Labels);
    }

    [Fact]
    public void Parse_PrettyPrintsRawJson()
    {
        ContainerInspection i = WslcCliProvider.ParseContainerInspection(RunningJson);

        Assert.Contains("random-logger", i.RawJson, StringComparison.Ordinal);
        Assert.Contains('\n', i.RawJson);
    }

    [Fact]
    public void Parse_EmptyArray_Throws()
        => Assert.Throws<EmptyCliResultException>(() => WslcCliProvider.ParseContainerInspection("[]"));
}
