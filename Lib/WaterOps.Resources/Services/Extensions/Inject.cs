using Avalonia.Controls;
using Microsoft.Extensions.DependencyInjection;
using WaterOps.Resources.Controls.ViewModels;
using WaterOps.Resources.Controls.Views;
using WaterOps.Resources.Interfaces;

namespace WaterOps.Resources.Services.Extensions;

public static class Inject
{
    public static void AddResources(this IServiceCollection collection, Window window)
    {
        collection.AddSingleton<INavigationService, NavigationService>();
        collection.AddSingleton<IViewModelFactory, ViewModelFactory>();
        collection.AddSingleton<IDialogService>(new DialogService(window));
    }
}
