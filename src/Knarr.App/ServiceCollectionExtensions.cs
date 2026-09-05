using Knarr.App.Features.Containers;
using Knarr.App.Features.Containers.ContainerInspect;
using Knarr.App.Features.Containers.ContainerLogs;
using Knarr.App.Features.Dashboard;
using Knarr.App.Features.Images;
using Knarr.App.Features.RunContainer;
using Knarr.App.Features.Settings;
using Knarr.App.Features.Shell;
using Knarr.App.Features.Sidebar;
using Knarr.App.Services;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace Knarr.App;

public static class ServiceCollectionExtensions
{
    extension(IServiceCollection collection)
    {
        public void AddCommonServices()
        {
            collection.AddLogging(builder => builder.AddSerilog(dispose: true));

            collection.AddSingleton<IThemeService, ThemeService>();
            collection.AddSingleton<IAutoRefreshService, AutoRefreshService>();
            collection.AddSingleton<IDialogService, DialogService>();

            collection.AddContainerServices();

            // Page view models are resolved through the container so they receive injected
            // services (ILogger, the CLI provider) when the sidebar navigates to them.
            collection.AddTransient<DashboardViewModel>();
            collection.AddTransient<ContainersViewModel>();
            collection.AddTransient<ImagesViewModel>();
            collection.AddTransient<SettingsViewModel>();

            // Dialog view models are transient so each open starts an independently-scoped session;
            // IDialogService resolves them on demand.
            collection.AddTransient<PullImageDialogViewModel>();
            collection.AddTransient<RunContainerDialogViewModel>();
            collection.AddTransient<ContainerLogsDialogViewModel>();
            collection.AddTransient<ImageInspectDialogViewModel>();
            collection.AddTransient<ContainerInspectDialogViewModel>();

            collection.AddTransient<SidebarViewModel>();
            collection.AddTransient<MainWindowViewModel>();
        }
    }
}
