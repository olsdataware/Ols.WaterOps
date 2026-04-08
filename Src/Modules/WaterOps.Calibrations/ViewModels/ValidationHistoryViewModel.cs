using CommunityToolkit.Mvvm.ComponentModel;
using WaterOps.Resources.Interfaces;

namespace WaterOps.Calibrations.ViewModels;

public partial class ValidationHistoryViewModel : ObservableObject, IViewModel
{
    public string? Title { get; set; } = "Validation History";
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
