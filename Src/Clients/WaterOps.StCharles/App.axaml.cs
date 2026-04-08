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
        var collection = new ServiceCollection();
        collection.AddRepositories(new DbContainer { Container = "StCharles" });
        collection.AddResources();
        collection.AddCalibrations();

        collection.AddScoped<MainView>();
        collection.AddScoped<MainViewModel>();

        var services = collection.BuildServiceProvider();

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var view = services.GetRequiredService<MainView>();
            view.DataContext = services.GetRequiredService<MainViewModel>();

            desktop.MainWindow = new MainWindow { MainHostContent = { Content = view } };
        }

        base.OnFrameworkInitializationCompleted();
    }
}
