namespace WaterOps.Previewer;

using ActiproSoftware.UI.Avalonia.Themes;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
        if (ModernTheme.TryGetCurrent(out var modernTheme) && (modernTheme.Definition is not null))
        {
            modernTheme.RefreshResources();
        }
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow { };
        }

        base.OnFrameworkInitializationCompleted();
    }
}
