using Microsoft.Extensions.DependencyInjection;
using WaterOps.Resources.Interfaces;

namespace WaterOps.Resources.Services.Extensions;

public static class Inject
{
    public static void AddResources(this IServiceCollection collection)
    {
        collection.AddSingleton<INavigationService, NavigationService>();
        collection.AddSingleton<IViewModelFactory, ViewModelFactory>();
    }
}
