using System.Threading;
using System.Threading.Tasks;
using Knarr.App.Services;
using Knarr.App.ViewModels;
using NSubstitute;
using Xunit;

namespace Knarr.App.Tests.ViewModels;

public class MainWindowViewModelTests
{
    private static MainWindowViewModel CreateViewModel(
        IPlatformInfoProvider? platformInfo = null,
        IThemeService? themeService = null)
    {
        platformInfo ??= Substitute.For<IPlatformInfoProvider>();
        themeService ??= Substitute.For<IThemeService>();
        return new MainWindowViewModel(platformInfo, themeService);
    }

    [Fact]
    public void NavigationItems_AreSeeded()
    {
        var vm = CreateViewModel();

        Assert.Equal(7, vm.NavigationItems.Count);
        Assert.Equal("Dashboard", vm.NavigationItems[0].Title);
        Assert.Equal("Settings", vm.NavigationItems[^1].Title);
    }

    [Fact]
    public void SelectedItem_DefaultsToDashboard()
    {
        var vm = CreateViewModel();

        Assert.NotNull(vm.SelectedItem);
        Assert.Equal("Dashboard", vm.SelectedItem!.Title);
    }

    [Fact]
    public void PlatformInfo_IsSurfacedFromProvider()
    {
        var platformInfo = Substitute.For<IPlatformInfoProvider>();
        platformInfo.PlatformName.Returns("macOS");
        platformInfo.CliName.Returns("apple container");

        var vm = CreateViewModel(platformInfo);

        Assert.Equal("macOS", vm.PlatformName);
        Assert.Equal("apple container", vm.CliName);
    }

    [Fact]
    public async Task InitializeAsync_SurfacesProbedCliVersion()
    {
        var platformInfo = Substitute.For<IPlatformInfoProvider>();
        platformInfo.CliName.Returns("apple container");
        platformInfo.IsCliReachable.Returns(true);
        platformInfo.CliVersion.Returns("v1.0.0");

        var vm = CreateViewModel(platformInfo);
        await vm.InitializeAsync();

        await platformInfo.Received(1).RefreshCliInfoAsync(Arg.Any<CancellationToken>());
        Assert.True(vm.IsCliReachable);
        Assert.Equal("v1.0.0", vm.CliVersion);
        Assert.Equal("apple container v1.0.0", vm.CliDisplay);
    }

    [Fact]
    public async Task InitializeAsync_WhenCliUnreachable_ReportsNotReachable()
    {
        var platformInfo = Substitute.For<IPlatformInfoProvider>();
        platformInfo.IsCliReachable.Returns(false);
        platformInfo.CliVersion.Returns("not detected");

        var vm = CreateViewModel(platformInfo);
        await vm.InitializeAsync();

        Assert.False(vm.IsCliReachable);
        Assert.Equal("not detected", vm.CliVersion);
    }

    [Fact]
    public void SetThemeCommand_DelegatesToThemeService()
    {
        var themeService = Substitute.For<IThemeService>();
        var vm = CreateViewModel(themeService: themeService);

        vm.SetThemeCommand.Execute(AppTheme.Dark);

        themeService.Received(1).SetTheme(AppTheme.Dark);
    }
}
