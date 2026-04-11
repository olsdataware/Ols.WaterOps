using CommunityToolkit.Mvvm.ComponentModel;
using WaterOps.Resources.Interfaces;

namespace WaterOps.Calibrations.ViewModels;

public partial class StandardsViewModel : ObservableObject, IViewModel
{
    public string? Title { get; set; } = "Standards";
    public bool IsDirty { get; set; }

    public async Task Initialize(object? parameter = null) { }

    public async Task Save() { }

    public Task Print() => Task.CompletedTask;

    public Task Delete() => Task.CompletedTask;
}
