using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Styling;
using IconPacks.Avalonia.PhosphorIcons;
using WaterOps.StCharles.ViewModels;

namespace WaterOps.StCharles.Views;

public partial class MainView : UserControl
{
    private readonly PackIconPhosphorIcons _dark = new()
    {
        Kind = PackIconPhosphorIconsKind.Moon,
        Height = 14,
        Width = 14,
    };

    private readonly PackIconPhosphorIcons _light = new()
    {
        Kind = PackIconPhosphorIconsKind.Sun,
        Height = 14,
        Width = 14,
    };

    public MainView()
    {
        InitializeComponent();
        ThemeButton.Content =
            Application.Current?.ActualThemeVariant == ThemeVariant.Dark ? _dark : _light;
    }

    protected override async void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);
        if (DataContext is MainViewModel viewModel)
        {
            await viewModel.InitializeAsync();
        }
    }

    private void MinimizeButton_Click(object? sender, RoutedEventArgs e)
    {
        if (TopLevel.GetTopLevel(this) is Window window)
        {
            window.WindowState = WindowState.Minimized;
        }
    }

    private void MaximizeButton_Click(object? sender, RoutedEventArgs e)
    {
        if (TopLevel.GetTopLevel(this) is not Window window)
            return;

        window.WindowState =
            window.WindowState == WindowState.Maximized
                ? WindowState.Normal
                : WindowState.Maximized;
    }

    private void CloseButton_Click(object? sender, RoutedEventArgs e)
    {
        if (TopLevel.GetTopLevel(this) is Window window)
        {
            window.Close();
        }
    }

    private void ThemeButton_Click(object? sender, RoutedEventArgs e)
    {
        var theme = Application.Current?.ActualThemeVariant;
        if (theme == ThemeVariant.Light)
        {
            Application.Current?.RequestedThemeVariant = ThemeVariant.Dark;
            ThemeButton.Content = _dark;
        }
        else
        {
            Application.Current?.RequestedThemeVariant = ThemeVariant.Light;
            ThemeButton.Content = _light;
        }
    }

    private void MenuButton_Click(object? sender, RoutedEventArgs e)
    {
        MainSplit.IsPaneOpen = true;
    }
}
