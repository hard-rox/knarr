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
    /// Registers the platform-appropriate <see cref="IContainerCliProvider"/>. Only Windows (wslc)
    /// is supported for now; other platforms throw <see cref="PlatformNotSupportedException"/>.
    /// </summary>
    public static IServiceCollection AddContainerServices(this IServiceCollection services)
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            throw new PlatformNotSupportedException(
                "Knarr currently supports only Windows (wslc). Other platforms are not yet implemented.");
        }

        services.AddSingleton<IContainerCliProvider, WslcContainerCliProvider>();
        return services;
    }
}
