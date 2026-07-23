using System.Runtime.InteropServices;
using Microsoft.Extensions.DependencyInjection;

namespace Knarr.Service;

/// <summary>
/// Registers the container service layer with the DI container. The concrete CLI provider stays
/// internal to this assembly; consumers depend only on <see cref="IContainerCliProvider"/>.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers the platform-appropriate <see cref="IContainerCliProvider"/>: Windows uses
    /// <c>wslc</c>, macOS uses Apple's <c>container</c> CLI. Other platforms throw
    /// <see cref="PlatformNotSupportedException"/>.
    /// </summary>
    public static IServiceCollection AddContainerServices(this IServiceCollection services)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            services.AddSingleton<IContainerCliProvider, WslcCli.WslcCliProvider>();
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            services.AddSingleton<IContainerCliProvider, AppleContainerCli.AppleContainerCliProvider>();
        }
        else
        {
            throw new PlatformNotSupportedException(
                "Knarr supports only Windows (wslc) and macOS (container). This platform is not implemented.");
        }

        return services;
    }
}
