using System;
using Knarr.App.Features.Containers;
using Knarr.App.Features.Dashboard;
using Knarr.App.Features.Images;
using Knarr.App.Features.Settings;
using Knarr.App.Features.Shell;
using Knarr.App.Features.Sidebar;
using Knarr.App.Services;
using Knarr.Service;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace Knarr.App.Tests.Features.Shell;

public class MainWindowViewModelTests
{
    private static IServiceProvider BuildServices(IContainerCliProvider cliProvider)
    {
        ServiceCollection services = new();
        services.AddLogging();
        services.AddSingleton(cliProvider);
        services.AddTransient<DashboardViewModel>();
        services.AddTransient<ContainersViewModel>();
        services.AddTransient<ImagesViewModel>();
        services.AddTransient<SettingsViewModel>();
        return services.BuildServiceProvider();
    }

    private static MainWindowViewModel CreateViewModel(
        IThemeService? themeService = null,
        SidebarViewModel? sidebar = null)
    {
        themeService ??= Substitute.For<IThemeService>();
        if (sidebar is null)
        {
            IContainerCliProvider cliProvider = Substitute.For<IContainerCliProvider>();
            sidebar = new SidebarViewModel(BuildServices(cliProvider), cliProvider, NullLogger<SidebarViewModel>.Instance);
        }

        return new MainWindowViewModel(themeService, sidebar, NullLogger<MainWindowViewModel>.Instance);
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
