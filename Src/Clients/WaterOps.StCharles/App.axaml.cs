using WaterOps.Resources.Services.Extensions;

namespace WaterOps.StCharles;

using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Calibrations.Services.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Repositories.Models;
using Repositories.Services.Extensions;
using Resources.Helpers;
using ViewModels;
using Views;

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
            var collection = new ServiceCollection();
            var window = new MainWindow();
            collection.AddResources(window);
            collection.AddRepositories(new DbContainer { Container = "StCharles" });
            collection.AddCalibrations();
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
