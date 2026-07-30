using Knarr.Service.AppleContainerCli;

namespace Knarr.Service.Tests;

/// <summary>
/// Unit tests for the Apple <c>container system status</c> payload parsing. Process execution is
/// delegated to CliWrap and is not exercised here; the internal static parse method is reached
/// through InternalsVisibleTo.
/// </summary>
public class AppleContainerSystemServiceTests
{
    [Fact]
    public void ParseStatus_WhenRunning_ShapesStateAndApiServerDetails()
    {
        const string json = """
        {
            "apiServerAppName": "container-apiserver",
            "apiServerBuild": "release",
            "apiServerCommit": "5973b9cc626a3e7a499bb316a958237ebe14e2ed",
            "apiServerVersion": "container-apiserver version 1.1.0 (build: release, commit: 5973b9c)",
            "appRoot": "/Users/roxy/Library/Application Support/com.apple.container/",
            "installRoot": "/usr/local/",
            "status": "running"
        }
        """;

        ContainerSystemStatus status = AppleContainerSystemService.ParseStatus(json);

        Assert.Equal(ContainerSystemState.Running, status.State);
        Assert.Equal("container-apiserver version 1.1.0 (build: release, commit: 5973b9c)", status.ApiServerVersion);
        Assert.Equal("/Users/roxy/Library/Application Support/com.apple.container/", status.AppRoot);
        Assert.Equal("/usr/local/", status.InstallRoot);
    }

    [Fact]
    public void ParseStatus_WhenUnregistered_ReportsUnregisteredWithEmptyDetails()
    {
        const string json = """
        {
            "apiServerAppName": "",
            "apiServerBuild": "",
            "apiServerCommit": "",
            "apiServerVersion": "",
            "appRoot": "",
            "installRoot": "",
            "status": "unregistered"
        }
        """;

        ContainerSystemStatus status = AppleContainerSystemService.ParseStatus(json);

        Assert.Equal(ContainerSystemState.Unregistered, status.State);
        Assert.Equal(string.Empty, status.ApiServerVersion);
        Assert.Equal(string.Empty, status.AppRoot);
    }

    [Theory]
    [InlineData("not running", ContainerSystemState.NotRunning)]
    [InlineData("stopped", ContainerSystemState.NotRunning)]
    [InlineData("RUNNING", ContainerSystemState.Running)]
    [InlineData("something else", ContainerSystemState.Unknown)]
    public void ParseStatus_MapsStatusStringCaseInsensitively(string status, ContainerSystemState expected)
    {
        var json = $$"""{ "status": "{{status}}" }""";

        Assert.Equal(expected, AppleContainerSystemService.ParseStatus(json).State);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("not json at all")]
    [InlineData("null")]
    public void ParseStatus_WhenOutputIsUnusable_ReturnsUnknown(string json)
    {
        ContainerSystemStatus status = AppleContainerSystemService.ParseStatus(json);

        Assert.Equal(ContainerSystemState.Unknown, status.State);
    }
}
