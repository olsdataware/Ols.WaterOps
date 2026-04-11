using Microsoft.Extensions.DependencyInjection;
using WaterOps.Updates.Interfaces;

namespace WaterOps.Updates.Services.Extensions;

public static class Inject
{
    public static IServiceCollection AddUpdates(
        this IServiceCollection services,
        string? container = null
    )
    {
        services.AddSingleton<IUpdateService>(sp =>
            !string.IsNullOrEmpty(container)
                ? (IUpdateService)new UpdateService(container)
                : (IUpdateService)new UpdateService("WaterOps")
        );
        return services;
    }
}
