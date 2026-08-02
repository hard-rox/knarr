using Microsoft.Extensions.Logging.Abstractions;

namespace Knarr.Service.Tests;

/// <summary>
/// Verifies that <c>image inspect</c> arg building produces the correct argument order for both providers.
/// </summary>
public class ImageInspectCommandTests
{
    private static WslcCli.WslcCliProvider CreateProvider()
        => new(NullLogger<WslcCli.WslcCliProvider>.Instance);

    private static AppleContainerCli.AppleContainerCliProvider CreateAppleProvider()
        => new(NullLogger<AppleContainerCli.AppleContainerCliProvider>.Instance);

    [Fact]
    public void Build_EmitsImageInspectThenReference()
        => Assert.Equal(["image", "inspect", "redis:latest"],
            CreateProvider().BuildImageInspectArgs("redis:latest"));

    [Fact]
    public void Build_TrimsReference()
        => Assert.Equal(["image", "inspect", "redis:latest"],
            CreateProvider().BuildImageInspectArgs("  redis:latest  "));

    [Fact]
    public void BuildCommand_Wslc()
        => Assert.Equal("wslc image inspect redis:latest",
            CreateProvider().BuildImageInspectCommand("redis:latest"));

    [Fact]
    public void BuildCommand_Apple()
        => Assert.Equal("container image inspect redis:latest",
            CreateAppleProvider().BuildImageInspectCommand("redis:latest"));
}
