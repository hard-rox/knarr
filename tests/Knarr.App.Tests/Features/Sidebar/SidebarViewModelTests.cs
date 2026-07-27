using System;
using System.Threading;
using System.Threading.Tasks;
using Knarr.App.Features.Containers;
using Knarr.App.Features.Dashboard;
using Knarr.App.Features.Images;
using Knarr.App.Features.Settings;
using Knarr.App.Features.Sidebar;
using Knarr.Service;
using Knarr.Service.Exceptions;
using Knarr.Service.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace Knarr.App.Tests.Features.Sidebar;

public class SidebarViewModelTests
{
    private static IServiceProvider BuildServices(
        IContainerCliProvider cliProvider,
        IContainerSystemService? systemService)
    {
        ServiceCollection services = new();
        services.AddLogging();
        services.AddSingleton(cliProvider);
        services.AddTransient<DashboardViewModel>();
        services.AddTransient<ContainersViewModel>();
        services.AddTransient<ImagesViewModel>();
        services.AddTransient<SettingsViewModel>();

        // Mirrors the runtime registration: present on macOS, absent elsewhere.
        if (systemService is not null)
        {
            services.AddSingleton(systemService);
        }

        return services.BuildServiceProvider();
    }

    private static SidebarViewModel CreateViewModel(
        IContainerCliProvider? cliProvider = null,
        IContainerSystemService? systemService = null)
    {
        cliProvider ??= Substitute.For<IContainerCliProvider>();
        return new SidebarViewModel(
            BuildServices(cliProvider, systemService), cliProvider, NullLogger<SidebarViewModel>.Instance);
    }

    private static IContainerSystemService CreateSystemService(ContainerSystemState state)
    {
        IContainerSystemService systemService = Substitute.For<IContainerSystemService>();
        systemService.GetStatusAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new ContainerSystemStatus { State = state }));
        return systemService;
    }

    /// <summary>A CLI provider whose platform probe succeeds, so <c>InitializeAsync</c> can run.</summary>
    private static IContainerCliProvider CreateProbeableCliProvider()
    {
        IContainerCliProvider cliProvider = Substitute.For<IContainerCliProvider>();
        cliProvider.GetPlatformInfoAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new PlatformInfo
            {
                PlatformName = "macOS",
                CliName = "container",
                CliVersion = "v1.1.0",
                IsCliReachable = true,
            }));
        return cliProvider;
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

    [Fact]
    public void SystemControl_IsHidden_WhenSystemServiceIsNotRegistered()
    {
        SidebarViewModel vm = CreateViewModel();

        Assert.False(vm.IsSystemControlVisible);
        Assert.False(vm.ToggleSystemCommand.CanExecute(null));
    }

    [Fact]
    public void SystemControl_IsVisible_WhenSystemServiceIsRegistered()
    {
        SidebarViewModel vm = CreateViewModel(systemService: CreateSystemService(ContainerSystemState.Running));

        Assert.True(vm.IsSystemControlVisible);
        Assert.True(vm.ToggleSystemCommand.CanExecute(null));
    }

    [Fact]
    public void IsCollapsedSystemControlVisible_TracksSidebarCollapse()
    {
        SidebarViewModel vm = CreateViewModel(systemService: CreateSystemService(ContainerSystemState.Running));

        Assert.False(vm.IsCollapsedSystemControlVisible);

        vm.ToggleSidebarCommand.Execute(null);
        Assert.True(vm.IsCollapsedSystemControlVisible);
    }

    [Fact]
    public async Task ToggleSystem_WhenRunning_StopsTheSystem()
    {
        IContainerSystemService systemService = CreateSystemService(ContainerSystemState.Running);
        SidebarViewModel vm = CreateViewModel(CreateProbeableCliProvider(), systemService);
        await vm.InitializeAsync();

        Assert.True(vm.IsSystemRunning);
        Assert.Equal("StopRegular", vm.SystemActionIcon);

        await vm.ToggleSystemCommand.ExecuteAsync(null);

        await systemService.Received(1).StopAsync(Arg.Any<CancellationToken>());
        await systemService.DidNotReceive().StartAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ToggleSystem_WhenUnregistered_StartsTheSystem()
    {
        IContainerSystemService systemService = CreateSystemService(ContainerSystemState.Unregistered);
        SidebarViewModel vm = CreateViewModel(CreateProbeableCliProvider(), systemService);
        await vm.InitializeAsync();

        Assert.False(vm.IsSystemRunning);
        Assert.Equal("PlayRegular", vm.SystemActionIcon);
        Assert.Equal("System unregistered", vm.SystemStatusText);

        await vm.ToggleSystemCommand.ExecuteAsync(null);

        await systemService.Received(1).StartAsync(Arg.Any<CancellationToken>());
        await systemService.DidNotReceive().StopAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ToggleSystem_WhenCommandFails_SurfacesErrorInline()
    {
        IContainerSystemService systemService = CreateSystemService(ContainerSystemState.NotRunning);
        systemService.StartAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromException(new CliCommandException("container system start", 1, "boom")));

        SidebarViewModel vm = CreateViewModel(CreateProbeableCliProvider(), systemService);
        await vm.InitializeAsync();

        await vm.ToggleSystemCommand.ExecuteAsync(null);

        Assert.NotNull(vm.SystemErrorMessage);
        Assert.Contains("boom", vm.SystemErrorMessage);
        Assert.Equal("Error", vm.SystemStatusText);
        Assert.False(vm.IsSystemBusy);
    }
}
