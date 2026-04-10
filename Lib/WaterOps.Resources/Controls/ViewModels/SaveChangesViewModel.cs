using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using WaterOps.Resources.Enums;
using WaterOps.Resources.Interfaces;

namespace WaterOps.Resources.Controls.ViewModels;

public partial class SaveChangesViewModel : ObservableObject, IDialogViewModel
{
    private readonly TaskCompletionSource<DialogResult> _source = new();
    public Task<DialogResult> Result => _source.Task;

    [RelayCommand]
    public void ExecuteYes() => _source.TrySetResult(DialogResult.Yes);

    [RelayCommand]
    public void ExecuteNo() => _source.TrySetResult(DialogResult.No);

    [RelayCommand]
    public void ExecuteCancel() => _source.TrySetResult(DialogResult.Cancel);
}
