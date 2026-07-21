using Knarr.App.Features.Dashboard;
using Knarr.App.Features.Settings;
using Knarr.App.Features.Shell;
using Knarr.App.Features.Sidebar;
using Knarr.App.Services;
using Knarr.Service;
using NSubstitute;

namespace Knarr.App.Tests.Features.Shell;

public class MainWindowViewModelTests
{
    private static MainWindowViewModel CreateViewModel(
        IThemeService? themeService = null,
        SidebarViewModel? sidebar = null)
    {
        themeService ??= Substitute.For<IThemeService>();
        sidebar ??= new SidebarViewModel(Substitute.For<IContainerCliProvider>());
        return new MainWindowViewModel(themeService, sidebar);
    }

    [Fact]
    public void NavigationItems_AreSeeded()
    {
        MainWindowViewModel vm = CreateViewModel();

        Assert.Equal(7, vm.Sidebar.NavigationItems.Count);
        Assert.Equal("Dashboard", vm.Sidebar.NavigationItems[0].Title);
        Assert.Equal("Settings", vm.Sidebar.NavigationItems[^1].Title);
    }

    [Fact]
    public void Sidebar_IsExpandedByDefault()
    {
        MainWindowViewModel vm = CreateViewModel();

        Assert.True(vm.Sidebar.IsSidebarExpanded);
    }

    [Fact]
    public void SetThemeCommand_DelegatesToThemeService()
    {
        IThemeService themeService = Substitute.For<IThemeService>();
        MainWindowViewModel vm = CreateViewModel(themeService: themeService);

        vm.SetThemeCommand.Execute(AppTheme.Dark);

        themeService.Received(1).SetTheme(AppTheme.Dark);
    }

    [Fact]
    public void CurrentPage_DefaultsToDashboardPage()
    {
        MainWindowViewModel vm = CreateViewModel();

        Assert.IsType<DashboardViewModel>(vm.CurrentPage);
    }

    [Fact]
    public void SelectingSettings_SwapsCurrentPageToSettings()
    {
        MainWindowViewModel vm = CreateViewModel();

        vm.Sidebar.SelectedItem = vm.Sidebar.NavigationItems[^1];

        Assert.IsType<SettingsViewModel>(vm.CurrentPage);
    }
}
