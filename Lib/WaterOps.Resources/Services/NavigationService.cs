using WaterOps.Resources.Interfaces;

namespace WaterOps.Resources.Services;

public class NavigationService(IViewModelFactory factory) : INavigationService
{
    private IViewModel? _viewModel;

    public IViewModel? ViewModel
    {
        get => _viewModel;
        private set
        {
            _viewModel = value;
            ViewModelChanged?.Invoke();
        }
    }

    public event Action? ViewModelChanged;

    public async Task NavigateToTypeAsync(Type viewModelType, object? parameter = null)
    {
        if (ViewModel is not null)
        {
            if (ViewModel.IsDirty)
            {
                await ViewModel.Save();
            }
        }

        var vm = factory.Create(viewModelType);
        await vm.Initialize(parameter);

        ViewModel = vm;
    }
}
