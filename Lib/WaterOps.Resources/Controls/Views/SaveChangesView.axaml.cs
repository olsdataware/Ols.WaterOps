using Avalonia.Controls;
using Avalonia.Interactivity;

namespace WaterOps.Resources.Controls.Views;

public partial class SaveChangesView : UserControl
{
    public SaveChangesView()
    {
        InitializeComponent();
    }

    private void CloseButton_OnClick(object? sender, RoutedEventArgs e)
    {
        if (TopLevel.GetTopLevel(this) is Window window)
        {
            window.Close();
        }
    }
}
