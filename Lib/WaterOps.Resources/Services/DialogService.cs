using Avalonia.Controls;
using WaterOps.Resources.Controls.Windows;
using WaterOps.Resources.Enums;
using WaterOps.Resources.Interfaces;

namespace WaterOps.Resources.Services;

public class DialogService(Window window) : IDialogService
{
    public async Task<DialogResult> ShowAsync(UserControl view, IDialogViewModel vm)
    {
        var dialog = new DialogWindow();
        view.DataContext = vm;
        dialog.DataContext = vm;

        var host = dialog.FindControl<ContentControl>("DialogHostContent");
        host?.Content = view;

        await dialog.ShowDialog(window);

        return await vm.Result;
    }
}
