using Knarr.App.Features.Containers;
using Knarr.App.Features.Dashboard;
using Knarr.App.Features.Images;
using Knarr.App.Features.Settings;
using Knarr.App.Features.Shell;
using Knarr.App.Features.Sidebar;
using Knarr.App.Services;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace Knarr.App;

/// <summary>
/// Registers the application's services and view models with the DI container.
/// </summary>
public static class ServiceCollectionExtensions
{
    extension(IServiceCollection collection)
    {
        public void AddCommonServices()
        {
            collection.AddLogging(builder => builder.AddSerilog(dispose: true));

            collection.AddSingleton<IThemeService, ThemeService>();

            collection.AddContainerServices();

            // Page view models are resolved through the container so they receive injected
            // services (ILogger, the CLI provider) when the sidebar navigates to them.
            collection.AddTransient<DashboardViewModel>();
            collection.AddTransient<ContainersViewModel>();
            collection.AddTransient<ImagesViewModel>();
            collection.AddTransient<SettingsViewModel>();

            collection.AddTransient<SidebarViewModel>();
            collection.AddTransient<MainWindowViewModel>();
        }
    }
}
