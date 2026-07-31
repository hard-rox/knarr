using System;
using Microsoft.Extensions.Logging.Abstractions;

namespace Knarr.Service.Tests;

/// <summary>
/// Verifies that <see cref="ContainerCliProviderBase.BuildLogsArgs"/> translates
/// <see cref="ContainerLogsOptions"/> into the exact, ordered <c>logs</c> argument list, for both
/// the wslc and Apple container providers (the argument builder is shared by the base class).
/// </summary>
public class ContainerLogsCommandTests
{
    private static WslcCli.WslcCliProvider CreateProvider()
        => new(NullLogger<WslcCli.WslcCliProvider>.Instance);

    private static AppleContainerCli.AppleContainerCliProvider CreateAppleProvider()
        => new(NullLogger<AppleContainerCli.AppleContainerCliProvider>.Instance);

    [Fact]
    public void Build_DefaultOptions_EmitsOnlyContainerId()
    {
        var provider = CreateProvider();

        string[] args = provider.BuildLogsArgs(new ContainerLogsOptions { ContainerId = "abc123" });

        Assert.Equal(["logs", "abc123"], args);
    }

    [Fact]
    public void Build_Follow_AddsFollowFlag()
    {
        var provider = CreateProvider();

        string[] args = provider.BuildLogsArgs(new ContainerLogsOptions { ContainerId = "abc123", Follow = true });

        Assert.Equal(["logs", "--follow", "abc123"], args);
    }

    [Fact]
    public void Build_Timestamps_AddsTimestampsFlag()
    {
        var provider = CreateProvider();

        string[] args = provider.BuildLogsArgs(new ContainerLogsOptions { ContainerId = "abc123", Timestamps = true });

        Assert.Equal(["logs", "--timestamps", "abc123"], args);
    }

    [Fact]
    public void Build_TailLines_AddsTailFlagWithCount()
    {
        var provider = CreateProvider();

        string[] args = provider.BuildLogsArgs(new ContainerLogsOptions { ContainerId = "abc123", TailLines = 200 });

        Assert.Equal(["logs", "--tail", "200", "abc123"], args);
    }

    [Fact]
    public void Build_SinceAndUntil_FormatAsRfc3339Utc()
    {
        var provider = CreateProvider();

        string[] args = provider.BuildLogsArgs(new ContainerLogsOptions
        {
            ContainerId = "abc123",
            Since = new DateTimeOffset(2024, 1, 15, 10, 30, 0, TimeSpan.Zero),
            Until = new DateTimeOffset(2024, 1, 15, 12, 0, 0, TimeSpan.Zero),
        });

        Assert.Equal(
            ["logs", "--since", "2024-01-15T10:30:00Z", "--until", "2024-01-15T12:00:00Z", "abc123"],
            args);
    }

    [Fact]
    public void Build_AllOptions_OrdersArgumentsAndIdLast()
    {
        var provider = CreateProvider();

        string[] args = provider.BuildLogsArgs(new ContainerLogsOptions
        {
            ContainerId = "abc123",
            Follow = true,
            Timestamps = true,
            TailLines = 50,
            Since = new DateTimeOffset(2024, 1, 15, 10, 30, 0, TimeSpan.Zero),
        });

        Assert.Equal(
            ["logs", "--follow", "--timestamps", "--tail", "50", "--since", "2024-01-15T10:30:00Z", "abc123"],
            args);
    }

    [Fact]
    public void Build_OnAppleProvider_UsesSameLogsVerb()
    {
        var provider = CreateAppleProvider();

        string[] args = provider.BuildLogsArgs(new ContainerLogsOptions { ContainerId = "abc123", Follow = true });

        Assert.Equal(["logs", "--follow", "abc123"], args);
    }

    [Fact]
    public void Build_OnAppleProvider_UsesShortTailFlag()
    {
        var provider = CreateAppleProvider();

        string[] args = provider.BuildLogsArgs(new ContainerLogsOptions { ContainerId = "abc123", TailLines = 200 });

        Assert.Equal(["logs", "-n", "200", "abc123"], args);
    }

    [Fact]
    public void Build_OnAppleProvider_Boot_AddsBootFlagBeforeFollow()
    {
        var provider = CreateAppleProvider();

        string[] args = provider.BuildLogsArgs(new ContainerLogsOptions { ContainerId = "abc123", Boot = true, Follow = true });

        Assert.Equal(["logs", "--boot", "--follow", "abc123"], args);
    }

    [Fact]
    public void Build_OnAppleProvider_DropsUnsupportedOptions()
    {
        var provider = CreateAppleProvider();

        string[] args = provider.BuildLogsArgs(new ContainerLogsOptions
        {
            ContainerId = "abc123",
            Timestamps = true,
            Since = new DateTimeOffset(2024, 1, 15, 10, 30, 0, TimeSpan.Zero),
            Until = new DateTimeOffset(2024, 1, 15, 12, 0, 0, TimeSpan.Zero),
        });

        Assert.Equal(["logs", "abc123"], args);
    }

    [Fact]
    public void Build_OnWslcProvider_IgnoresBoot()
    {
        var provider = CreateProvider();

        string[] args = provider.BuildLogsArgs(new ContainerLogsOptions { ContainerId = "abc123", Boot = true });

        Assert.Equal(["logs", "abc123"], args);
    }
}
