using Microsoft.Extensions.DependencyInjection;
using WaterOps.Resources.Interfaces;

namespace WaterOps.Resources.Services;

public class ViewModelFactory(IServiceProvider serviceProvider) : IViewModelFactory
{
    public IViewModel Create(Type viewModelType)
    {
        var viewModel = serviceProvider.GetRequiredService(viewModelType);
        if (viewModel is IViewModel vm)
            return vm;
        else
        {
            throw new ArgumentException(
                "ViewModelFactory cannot create a ViewModel of type " + viewModelType.FullName
            );
        }
    }
}
