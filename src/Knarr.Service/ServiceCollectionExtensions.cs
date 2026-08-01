using System.Runtime.InteropServices;
using Microsoft.Extensions.DependencyInjection;

namespace Knarr.Service;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddContainerServices(this IServiceCollection services)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            services.AddSingleton<IContainerCliProvider, WslcCli.WslcCliProvider>();
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            services.AddSingleton<IContainerCliProvider, AppleContainerCli.AppleContainerCliProvider>();

            // `container system` is macOS-only; consumers resolve this optionally and hide the
            // corresponding UI on platforms where it is not registered.
            services.AddSingleton<IContainerSystemService, AppleContainerCli.AppleContainerSystemService>();
        }
        else
        {
            throw new PlatformNotSupportedException(
                "Knarr supports only Windows (wslc) and macOS (container). This platform is not implemented.");
        }

        return services;
    }
}
