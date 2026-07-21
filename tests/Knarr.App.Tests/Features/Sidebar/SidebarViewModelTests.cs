using System.Threading;
using System.Threading.Tasks;
using Knarr.App.Features.Sidebar;
using Knarr.Service;
using Knarr.Service.Models;
using NSubstitute;

namespace Knarr.App.Tests.Features.Sidebar;

public class SidebarViewModelTests
{
    private static SidebarViewModel CreateViewModel(IContainerCliProvider? cliProvider = null)
    {
        cliProvider ??= Substitute.For<IContainerCliProvider>();
        return new SidebarViewModel(cliProvider);
    }

    [Fact]
    public void NavigationItems_AreSeeded()
    {
        SidebarViewModel vm = CreateViewModel();

        Assert.Equal(7, vm.NavigationItems.Count);
        Assert.Equal("Dashboard", vm.NavigationItems[0].Title);
        Assert.Equal("Settings", vm.NavigationItems[^1].Title);
    }

    [Fact]
    public void NavigationItems_UseIconResourceKeys()
    {
        SidebarViewModel vm = CreateViewModel();

        Assert.Equal("BoardRegular", vm.NavigationItems[0].Icon);
        Assert.Equal("SettingsRegular", vm.NavigationItems[^1].Icon);
    }

    [Fact]
    public void SelectedItem_DefaultsToDashboard()
    {
        SidebarViewModel vm = CreateViewModel();

        Assert.NotNull(vm.SelectedItem);
        Assert.Equal("Dashboard", vm.SelectedItem!.Title);
    }

    [Fact]
    public void IsExpandedByDefault()
    {
        SidebarViewModel vm = CreateViewModel();

        Assert.True(vm.IsSidebarExpanded);
    }

    [Fact]
    public void ToggleSidebar_FlipsIsSidebarExpanded()
    {
        SidebarViewModel vm = CreateViewModel();

        vm.ToggleSidebarCommand.Execute(null);
        Assert.False(vm.IsSidebarExpanded);

        vm.ToggleSidebarCommand.Execute(null);
        Assert.True(vm.IsSidebarExpanded);
    }

    [Fact]
    public async Task InitializeAsync_SurfacesProbedPlatformInfo()
    {
        IContainerCliProvider cliProvider = Substitute.For<IContainerCliProvider>();
        cliProvider.GetPlatformInfoAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new PlatformInfo
            {
                PlatformName = "Windows",
                CliName = "wslc",
                CliVersion = "v2.9.3.0",
                IsCliReachable = true,
            }));

        SidebarViewModel vm = CreateViewModel(cliProvider);
        await vm.InitializeAsync();

        await cliProvider.Received(1).GetPlatformInfoAsync(Arg.Any<CancellationToken>());
        Assert.Equal("Windows", vm.PlatformName);
        Assert.Equal("wslc", vm.CliName);
        Assert.True(vm.IsCliReachable);
        Assert.Equal("v2.9.3.0", vm.CliVersion);
        Assert.Equal("wslc v2.9.3.0", vm.CliDisplay);
    }

    [Fact]
    public async Task InitializeAsync_WhenCliUnreachable_ReportsNotReachable()
    {
        IContainerCliProvider cliProvider = Substitute.For<IContainerCliProvider>();
        cliProvider.GetPlatformInfoAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new PlatformInfo
            {
                PlatformName = "Windows",
                CliName = "wslc",
                CliVersion = "not detected",
                IsCliReachable = false,
            }));

        SidebarViewModel vm = CreateViewModel(cliProvider);
        await vm.InitializeAsync();

        Assert.False(vm.IsCliReachable);
        Assert.Equal("not detected", vm.CliVersion);
    }
}
