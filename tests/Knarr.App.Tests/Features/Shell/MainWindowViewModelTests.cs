using Knarr.App.Features.Dashboard;
using Knarr.App.Features.Settings;
using Knarr.App.Features.Shell;
using Knarr.App.Features.Sidebar;
using Knarr.App.Services;
using NSubstitute;
using Xunit;

namespace Knarr.App.Tests.Features.Shell;

public class MainWindowViewModelTests
{
    private static MainWindowViewModel CreateViewModel(
        IThemeService? themeService = null,
        SidebarViewModel? sidebar = null)
    {
        themeService ??= Substitute.For<IThemeService>();
        sidebar ??= new SidebarViewModel(Substitute.For<IPlatformInfoProvider>());
        return new MainWindowViewModel(themeService, sidebar);
    }

    [Fact]
    public void NavigationItems_AreSeeded()
    {
        var vm = CreateViewModel();

        Assert.Equal(7, vm.Sidebar.NavigationItems.Count);
        Assert.Equal("Dashboard", vm.Sidebar.NavigationItems[0].Title);
        Assert.Equal("Settings", vm.Sidebar.NavigationItems[^1].Title);
    }

    [Fact]
    public void Sidebar_IsExpandedByDefault()
    {
        var vm = CreateViewModel();

        Assert.True(vm.Sidebar.IsSidebarExpanded);
    }

    [Fact]
    public void SetThemeCommand_DelegatesToThemeService()
    {
        var themeService = Substitute.For<IThemeService>();
        var vm = CreateViewModel(themeService: themeService);

        vm.SetThemeCommand.Execute(AppTheme.Dark);

        themeService.Received(1).SetTheme(AppTheme.Dark);
    }

    [Fact]
    public void CurrentPage_DefaultsToDashboardPage()
    {
        var vm = CreateViewModel();

        Assert.IsType<DashboardViewModel>(vm.CurrentPage);
    }

    [Fact]
    public void SelectingSettings_SwapsCurrentPageToSettings()
    {
        var vm = CreateViewModel();

        vm.Sidebar.SelectedItem = vm.Sidebar.NavigationItems[^1];

        Assert.IsType<SettingsViewModel>(vm.CurrentPage);
    }

    [Fact]
    public void SelectingItemWithoutPage_ClearsCurrentPage()
    {
        var vm = CreateViewModel();

        // "Containers" has no page factory yet.
        vm.Sidebar.SelectedItem = vm.Sidebar.NavigationItems[1];

        Assert.Null(vm.CurrentPage);
    }
}
