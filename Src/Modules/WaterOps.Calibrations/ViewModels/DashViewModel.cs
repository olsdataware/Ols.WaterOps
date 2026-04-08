using CommunityToolkit.Mvvm.ComponentModel;
using WaterOps.Resources.Interfaces;

namespace WaterOps.Calibrations.ViewModels;

public partial class DashViewModel : ObservableObject, IViewModel
{
    public string? Title { get; set; } = "Calibration & Validation Dashboard";
    public bool IsDirty { get; set; }

    public async Task Initialize(object? parameter = null)
    {
        // init fields
    }

    public Task Save()
    {
        throw new NotImplementedException();
    }

    public Task Print()
    {
        throw new NotImplementedException();
    }

    public Task Delete()
    {
        throw new NotImplementedException();
    }
}
