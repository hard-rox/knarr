using System.Runtime.InteropServices;
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
    extension(IServiceCollection collection)
    {
        public void AddCommonServices()
        {
            collection.AddSingleton<IPlatformInfoProvider, PlatformInfoProvider>();
            collection.AddSingleton<IThemeService, ThemeService>();

            collection.AddSingleton<ICliProcessRunner, CliProcessRunner>();
            collection.AddContainerCliProvider();

            collection.AddTransient<SidebarViewModel>();
            collection.AddTransient<MainWindowViewModel>();
        }

        /// <summary>
        /// Registers the platform-appropriate <see cref="IContainerCliProvider"/>: Apple Container on
        /// macOS, wslc on Windows. Other hosts (e.g. Linux dev boxes) fall back to the sample provider
        /// so the app still runs without a supported CLI.
        /// </summary>
        private void AddContainerCliProvider()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                collection.AddSingleton<IContainerCliProvider, AppleContainerCliProvider>();
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                collection.AddSingleton<IContainerCliProvider, WslcCliProvider>();
            }
            else
            {
                collection.AddSingleton<IContainerCliProvider, DesignTimeContainerCliProvider>();
            }
        }
    }
}
