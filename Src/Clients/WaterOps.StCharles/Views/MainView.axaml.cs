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
    public MainView()
    {
        InitializeComponent();
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

    private void MenuButton_Click(object? sender, RoutedEventArgs e)
    {
        MainSplit.IsPaneOpen = true;
    }

    private void Border_PointerPressed(object? sender, Avalonia.Input.PointerPressedEventArgs e)
    {
        if (TopLevel.GetTopLevel(this) is Window window)
        {
            window.BeginMoveDrag(e);
        }
    }

    private void ThemeButton_Click(object? sender, RoutedEventArgs e)
    {
        var theme = App.Current?.ActualThemeVariant;
        if (theme == ThemeVariant.Dark)
        {
            App.Current?.RequestedThemeVariant = ThemeVariant.Light;
        }
        else
        {
            App.Current?.RequestedThemeVariant = ThemeVariant.Dark;
        }
    }
}
