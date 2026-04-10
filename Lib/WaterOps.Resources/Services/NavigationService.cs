using WaterOps.Resources.Controls.ViewModels;
using WaterOps.Resources.Controls.Views;
using WaterOps.Resources.Enums;
using WaterOps.Resources.Interfaces;

namespace WaterOps.Resources.Services;

public class NavigationService(IViewModelFactory factory, IDialogService dialogService)
    : INavigationService
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
                IDialogViewModel dialogViewModel = new SaveChangesViewModel();
                var dialogView = new SaveChangesView();

                var result = await dialogService.ShowAsync(dialogView, dialogViewModel);

                switch (result)
                {
                    case DialogResult.Yes:
                        await ViewModel.Save();
                        break;
                    case DialogResult.Cancel:
                        return;
                }
            }
        }

        var vm = factory.Create(viewModelType);
        await vm.Initialize(parameter);

        ViewModel = vm;
    }
}
