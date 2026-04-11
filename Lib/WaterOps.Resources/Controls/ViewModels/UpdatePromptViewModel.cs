using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using WaterOps.Resources.Enums;
using WaterOps.Resources.Interfaces;

namespace WaterOps.Resources.Controls.ViewModels;

public partial class UpdatePromptViewModel : ObservableObject, IDialogViewModel
{
    private readonly TaskCompletionSource<DialogResult> _source = new();
    public Task<DialogResult> Result => _source.Task;

    [ObservableProperty]
    public partial string Version { get; set; } = string.Empty;

    [RelayCommand]
    public void ExecuteYes() => _source.TrySetResult(DialogResult.Yes);

    [RelayCommand]
    public void ExecuteNo() => _source.TrySetResult(DialogResult.No);
}
