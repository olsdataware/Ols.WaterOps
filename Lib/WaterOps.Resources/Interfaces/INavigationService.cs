namespace WaterOps.Resources.Interfaces;

public interface INavigationService
{
    public IViewModel? ViewModel { get; }
    public event Action? ViewModelChanged;
    public Task NavigateToTypeAsync(Type viewModelType, object? parameter = null);
}
