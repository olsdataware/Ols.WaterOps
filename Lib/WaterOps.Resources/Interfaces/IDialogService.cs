using Avalonia.Controls;
using WaterOps.Resources.Enums;

namespace WaterOps.Resources.Interfaces;

public interface IDialogService
{
    Task<DialogResult> ShowAsync(UserControl view, IDialogViewModel vm);
}
