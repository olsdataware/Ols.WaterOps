using Avalonia.Controls;
using Avalonia.Interactivity;

namespace WaterOps.Resources.Controls.Views;

public partial class UpdatePromptView : UserControl
{
    public UpdatePromptView()
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
