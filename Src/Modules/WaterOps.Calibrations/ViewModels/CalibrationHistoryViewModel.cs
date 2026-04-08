using CommunityToolkit.Mvvm.ComponentModel;
using WaterOps.Resources.Interfaces;

namespace WaterOps.Calibrations.ViewModels;

public partial class CalibrationHistoryViewModel : ObservableObject, IViewModel
{
    public string? Title { get; set; } = "Calibration History";
    public bool IsDirty { get; set; }

    public Task Initialize(object? parameter = null)
    {
        throw new NotImplementedException();
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
