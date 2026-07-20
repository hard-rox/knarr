using System.Runtime.InteropServices;
using Microsoft.Extensions.DependencyInjection;

namespace Knarr.Service;

/// <summary>
/// Registers the container service layer with the DI container. Keeps the concrete CLI providers
/// internal to this assembly; consumers depend only on the exposed interfaces.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers the platform-appropriate <see cref="IContainerCliProvider"/> and
    /// <see cref="IPlatformInfoProvider"/>: Apple Container on macOS, wslc on Windows. Other hosts
    /// (e.g. Linux dev boxes) fall back to the sample provider so the app still runs without a
    /// supported CLI.
    /// </summary>
    public static IServiceCollection AddContainerServices(this IServiceCollection services)
    {
        services.AddSingleton<IPlatformInfoProvider, PlatformInfoProvider>();

        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            services.AddSingleton<IContainerCliProvider, AppleContainerCliProvider>();
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            services.AddSingleton<IContainerCliProvider, WslcCliProvider>();
        }
        else
        {
            services.AddSingleton<IContainerCliProvider, DesignTimeContainerCliProvider>();
        }

        return services;
    }
}
