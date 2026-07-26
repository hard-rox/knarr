using Microsoft.Extensions.Logging.Abstractions;

namespace Knarr.Service.Tests;

/// <summary>
/// Verifies that <see cref="IContainerCliProvider.BuildRunContainerCommand"/> translates run
/// options into the exact, ordered CLI argument list. Exercised against the wslc provider (the
/// base-class argument builder is shared by both platforms).
/// </summary>
public class RunContainerCommandTests
{
    private static IContainerCliProvider CreateProvider()
        => new WslcCli.WslcCliProvider(NullLogger<WslcCli.WslcCliProvider>.Instance);

    [Fact]
    public void Build_DefaultDetached_EmitsDetachFlagAndImageLast()
    {
        IContainerCliProvider provider = CreateProvider();

        var command = provider.BuildRunContainerCommand(new RunContainerOptions
        {
            ImageReference = "alpine:3.20",
        });

        Assert.Equal("wslc run --detach alpine:3.20", command);
    }

    [Fact]
    public void Build_ForegroundWithRemove_OmitsDetachAndAddsRm()
    {
        IContainerCliProvider provider = CreateProvider();

        var command = provider.BuildRunContainerCommand(new RunContainerOptions
        {
            ImageReference = "nginx:latest",
            Detach = false,
            RemoveOnExit = true,
        });

        Assert.Equal("wslc run --rm nginx:latest", command);
    }

    [Fact]
    public void Build_WithNameEnvAndVolumes_OrdersArgumentsAndImageLast()
    {
        IContainerCliProvider provider = CreateProvider();

        var command = provider.BuildRunContainerCommand(new RunContainerOptions
        {
            ImageReference = "redis:7",
            Detach = true,
            RemoveOnExit = false,
            Name = "cache",
            EnvironmentVariables =
            [
                new RunEnvironmentVariable("KEY", "value"),
                new RunEnvironmentVariable("OTHER", "2"),
            ],
            Volumes =
            [
                new RunVolumeMount("/host/data", "/data"),
            ],
            Ports =
            [
                new RunPortMapping("8080", "80"),
            ],
        });

        Assert.Equal(
            "wslc run --detach --name cache --env KEY=value --env OTHER=2 --volume /host/data:/data --publish 8080:80 redis:7",
            command);
    }

    [Fact]
    public void Build_SkipsBlankEnvKeysAndIncompleteVolumes()
    {
        IContainerCliProvider provider = CreateProvider();

        var command = provider.BuildRunContainerCommand(new RunContainerOptions
        {
            ImageReference = "busybox",
            EnvironmentVariables =
            [
                new RunEnvironmentVariable("   ", "ignored"),
                new RunEnvironmentVariable("OK", "1"),
            ],
            Volumes =
            [
                new RunVolumeMount("/only-source", "   "),
                new RunVolumeMount("/a", "/b"),
            ],
            Ports =
            [
                new RunPortMapping("9090", "   "),
                new RunPortMapping("5000", "5000"),
            ],
        });

        Assert.Equal("wslc run --detach --env OK=1 --volume /a:/b --publish 5000:5000 busybox", command);
    }
}
