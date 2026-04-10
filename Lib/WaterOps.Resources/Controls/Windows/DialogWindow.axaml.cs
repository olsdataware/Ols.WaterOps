using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using WaterOps.Resources.Interfaces;

namespace WaterOps.Resources.Controls.Windows;

public partial class DialogWindow : Window
{
    public DialogWindow()
    {
        InitializeComponent();
    }

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);

        if (DataContext is IDialogViewModel vm)
        {
            vm.Result.ContinueWith(
                _ => Dispatcher.UIThread.Post(Close),
                TaskScheduler.FromCurrentSynchronizationContext()
            );
        }
    }
}
