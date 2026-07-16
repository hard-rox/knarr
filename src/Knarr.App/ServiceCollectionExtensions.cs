using Knarr.App.Features.Shell;
using Knarr.App.Features.Sidebar;
using Knarr.App.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Knarr.App;

/// <summary>
/// Registers the application's services and view models with the DI container.
/// </summary>
public static class ServiceCollectionExtensions
{
    public static void AddCommonServices(this IServiceCollection collection)
    {
        collection.AddSingleton<IPlatformInfoProvider, PlatformInfoProvider>();
        collection.AddSingleton<IThemeService, ThemeService>();

        collection.AddTransient<SidebarViewModel>();
        collection.AddTransient<MainWindowViewModel>();
    }
}
