using Microsoft.Extensions.DependencyInjection;
using WaterOps.Calibrations.ViewModels;
using WaterOps.Calibrations.Views;

namespace WaterOps.Calibrations.Services.Extensions;

public static class Inject
{
    public static IServiceCollection AddCalibrations(this IServiceCollection collection)
    {
        collection.AddTransient<CalibrationDetailsViewModel>();
        collection.AddTransient<CalibrationDetailsView>();
        collection.AddTransient<CalibrationHistoryViewModel>();
        collection.AddTransient<CalibrationHistoryView>();
        collection.AddTransient<DashViewModel>();
        collection.AddTransient<DashView>();
        collection.AddTransient<InstrumentsViewModel>();
        collection.AddTransient<InstrumentsView>();
        collection.AddTransient<StandardsViewModel>();
        collection.AddTransient<StandardsView>();
        collection.AddTransient<ValidationDetailsViewModel>();
        collection.AddTransient<ValidationDetailsView>();

        return collection;
    }
}
