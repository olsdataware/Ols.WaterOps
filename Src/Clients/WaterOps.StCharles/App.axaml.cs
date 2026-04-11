using WaterOps.Resources.Services.Extensions;

namespace WaterOps.StCharles;

using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using Calibrations.Services.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Repositories.Models;
using Repositories.Services.Extensions;
using Resources.Helpers;
using ViewModels;
using Views;
using WaterOps.Updates.Interfaces;
using WaterOps.Updates.Services.Extensions;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
        Themes.Refresh();
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var window = new MainWindow();

            var collection = new ServiceCollection()
                .AddResources(window)
                .AddRepositories(new DbContainer { Container = "StCharles" })
                .AddCalibrations()
                .AddUpdates("StCharles");

            collection.AddScoped<MainView>();
            collection.AddScoped<MainViewModel>();

            var services = collection.BuildServiceProvider();

            var view = services.GetRequiredService<MainView>();
            var vm = services.GetRequiredService<MainViewModel>();
            view.DataContext = vm;

            window.MainHostContent.Content = view;
            desktop.MainWindow = window;
        }

        base.OnFrameworkInitializationCompleted();
    }
}
