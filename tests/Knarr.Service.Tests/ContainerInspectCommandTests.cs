using Microsoft.Extensions.Logging.Abstractions;

namespace Knarr.Service.Tests;

/// <summary>
/// Verifies that <c>container inspect</c> arg building produces the correct argument order for both providers.
/// </summary>
public class ContainerInspectCommandTests
{
    private static WslcCli.WslcCliProvider CreateProvider()
        => new(NullLogger<WslcCli.WslcCliProvider>.Instance);

    private static AppleContainerCli.AppleContainerCliProvider CreateAppleProvider()
        => new(NullLogger<AppleContainerCli.AppleContainerCliProvider>.Instance);

    [Fact]
    public void Build_EmitsContainerInspectThenId()
        => Assert.Equal(["container", "inspect", "abc123"],
            CreateProvider().BuildContainerInspectArgs("abc123"));

    [Fact]
    public void Build_TrimsContainerId()
        => Assert.Equal(["container", "inspect", "abc123"],
            CreateProvider().BuildContainerInspectArgs("  abc123  "));

    [Fact]
    public void BuildCommand_Wslc()
        => Assert.Equal("wslc container inspect abc123",
            CreateProvider().BuildContainerInspectCommand("abc123"));

    [Fact]
    public void BuildCommand_Apple()
        => Assert.Equal("container inspect abc123",
            CreateAppleProvider().BuildContainerInspectCommand("abc123"));
}
