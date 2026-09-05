using System;
using System.Linq;
using Knarr.Service.Exceptions;
using Knarr.Service.WslcCli;

namespace Knarr.Service.Tests;

/// <summary>
/// Verifies that the wslc <c>image inspect</c> payload is shaped into <see cref="ImageInspection"/>.
/// </summary>
public class ImageInspectParsingTests
{
    private const string RedisJson = """
    [{
        "Architecture": "amd64",
        "Author": "",
        "Comment": "buildkit.dockerfile.v0",
        "Config": {
            "Cmd": ["redis-server"],
            "Entrypoint": ["docker-entrypoint.sh"],
            "Env": ["PATH=/usr/local/sbin:/usr/local/bin", "REDIS_VERSION=8.8.1"],
            "ExposedPorts": { "6379/tcp": {} },
            "Labels": null,
            "StopSignal": "",
            "User": "",
            "Volumes": null,
            "WorkingDir": "/data"
        },
        "Created": "2026-07-24T17:21:06.644209446Z",
        "Id": "sha256:d9f3c2aa7439bef41152d378fbd0f569f6c751db9b782fb63598cf23fd125c51",
        "Os": "linux",
        "Parent": "",
        "RepoDigests": ["redis@sha256:c88d347edef6249a6d2293f926f1eeb48bd40c57cbcd02c07f52e7f1fd2cb46b"],
        "RepoTags": ["redis:latest"],
        "RootFS": { "Layers": ["sha256:aaa", "sha256:bbb"], "Type": "layers" },
        "Size": 142598340
    }]
    """;

    private const string SeqJson = """
    [{
        "Architecture": "amd64",
        "Config": {
            "Cmd": null,
            "Entrypoint": ["/bin/seqentry"],
            "Env": ["ACCEPT_EULA=N", "BASE_URI=", "TZ=Etc/UTC"],
            "ExposedPorts": { "443/tcp": {}, "45341/tcp": {}, "5341/tcp": {}, "80/tcp": {} },
            "Labels": { "Description": "Seq", "Vendor": "Datalust Pty Ltd", "org.opencontainers.image.version": "24.04" },
            "Volumes": { "/data": {} }
        },
        "Id": "sha256:5285e36f0747",
        "Os": "linux",
        "RepoTags": ["datalust/seq:latest"],
        "Size": 616517308
    }]
    """;

    private const string RabbitJson = """
    [{
        "Config": {
            "Env": ["RABBITMQ_PGP_KEY_ID=0x0A9AF2115F4687BD29803A206B73A36E6026DFCA", "LANG=C.UTF-8"]
        },
        "Id": "sha256:17b158803d55",
        "RepoTags": ["rabbitmq:management"],
        "Size": 250592881
    }]
    """;

    [Fact]
    public void Parse_ShapesIdentityAndMetadata()
    {
        ImageInspection i = WslcCliProvider.ParseImageInspection(RedisJson);

        Assert.Equal("sha256:d9f3c2aa7439bef41152d378fbd0f569f6c751db9b782fb63598cf23fd125c51", i.Id);
        Assert.Equal("d9f3c2aa7439", i.ShortId);
        Assert.Equal(["redis:latest"], i.RepoTags);
        Assert.Single(i.RepoDigests);
        Assert.Equal("amd64", i.Architecture);
        Assert.Equal("linux", i.Os);
        Assert.Equal(142598340, i.SizeBytes);
        Assert.Equal(new DateTime(2026, 7, 24), i.Created!.Value.UtcDateTime.Date);
        Assert.Equal("/data", i.WorkingDirectory);
    }

    [Fact]
    public void Parse_MapsConfig()
    {
        ImageInspection i = WslcCliProvider.ParseImageInspection(RedisJson);

        Assert.Equal(["docker-entrypoint.sh"], i.Entrypoint);
        Assert.Equal(["redis-server"], i.Command);
        Assert.Equal(["6379/tcp"], i.ExposedPorts);
    }

    [Fact]
    public void Parse_NullCollections_BecomeEmpty()
    {
        ImageInspection redis = WslcCliProvider.ParseImageInspection(RedisJson);
        ImageInspection seq = WslcCliProvider.ParseImageInspection(SeqJson);

        Assert.Empty(redis.Labels);
        Assert.Empty(redis.Volumes);
        Assert.Empty(seq.Command);
    }

    [Fact]
    public void Parse_SplitsEnvironmentOnFirstEqualsOnly()
    {
        ImageInspection i = WslcCliProvider.ParseImageInspection(RabbitJson);

        InspectionEntry entry = i.EnvironmentVariables.First(e => e.Key == "RABBITMQ_PGP_KEY_ID");
        Assert.Equal("0x0A9AF2115F4687BD29803A206B73A36E6026DFCA", entry.Value);
    }

    [Fact]
    public void Parse_EmptyEnvironmentValue_IsPreserved()
    {
        ImageInspection i = WslcCliProvider.ParseImageInspection(SeqJson);

        Assert.Equal(string.Empty, i.EnvironmentVariables.First(e => e.Key == "BASE_URI").Value);
    }

    [Fact]
    public void Parse_SortsExposedPortsNumerically()
    {
        ImageInspection i = WslcCliProvider.ParseImageInspection(SeqJson);

        Assert.Equal(["80/tcp", "443/tcp", "5341/tcp", "45341/tcp"], i.ExposedPorts);
    }

    [Fact]
    public void Parse_MapsLabelsAndVolumes()
    {
        ImageInspection i = WslcCliProvider.ParseImageInspection(SeqJson);

        Assert.Equal(3, i.Labels.Count);
        Assert.Contains(i.Labels, l => l is { Key: "Vendor", Value: "Datalust Pty Ltd" });
        Assert.Equal(["/data"], i.Volumes);
    }

    [Fact]
    public void Parse_PrettyPrintsRawJson()
    {
        ImageInspection i = WslcCliProvider.ParseImageInspection(RabbitJson);

        Assert.Contains("rabbitmq:management", i.RawJson, StringComparison.Ordinal);
        Assert.Contains('\n', i.RawJson);
    }

    [Fact]
    public void Parse_EmptyArray_Throws()
        => Assert.Throws<EmptyCliResultException>(() => WslcCliProvider.ParseImageInspection("[]"));
}
