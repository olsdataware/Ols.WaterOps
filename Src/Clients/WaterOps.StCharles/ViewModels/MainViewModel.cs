using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using WaterOps.Calibrations.ViewModels;
using WaterOps.Core.Models;
using WaterOps.Resources.Interfaces;

namespace WaterOps.StCharles.ViewModels;

public partial class MainViewModel : ObservableObject
{
    [ObservableProperty]
    public partial bool CanSave { get; set; }

    [ObservableProperty]
    public partial bool CanPrint { get; set; }

    [ObservableProperty]
    public partial ObservableCollection<PwsInformation> PwsList { get; set; } =
        [
            new PwsInformation { PwsId = "LA1089001-E", PwsName = "East Bank, St. Charles Parish" },
            new PwsInformation { PwsId = "LA1089001-W", PwsName = "West Bank, St. Charles Parish" },
        ];

    [ObservableProperty]
    public partial PwsInformation? Pws { get; set; }

    partial void OnPwsChanged(PwsInformation? value)
    {
        if (value is not null)
        {
            RefreshCommand.Execute(null);
        }
    }

    [ObservableProperty]
    public partial DateTime Date { get; set; } = DateTime.Today;

    partial void OnDateChanged(DateTime value)
    {
        RefreshCommand.Execute(null);
    }

    public IViewModel? ViewModel => _navigationService.ViewModel;
    private readonly INavigationService _navigationService;
    private readonly Dictionary<string, Type> _routes = new()
    {
        { "CalibrationDetails", typeof(CalibrationDetailsViewModel) },
        { "CalibrationHistory", typeof(CalibrationHistoryViewModel) },
        { "Dashboard", typeof(DashViewModel) },
        { "Instruments", typeof(InstrumentsViewModel) },
        { "Standards", typeof(StandardsViewModel) },
        { "ValidationDetails", typeof(ValidationDetailsViewModel) },
        { "ValidationHistory", typeof(ValidationHistoryViewModel) },
    };

    public MainViewModel(INavigationService navigationService)
    {
        _navigationService = navigationService;
        _navigationService.ViewModelChanged += OnViewModelChanged;
    }

    public async Task InitializeAsync()
    {
        Pws = PwsList[0];

        try
        {
            await NavigateAsync("Dashboard");
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }

    private void OnViewModelChanged()
    {
        OnPropertyChanged(nameof(ViewModel));
    }

    [RelayCommand]
    private async Task NavigateAsync(string vm)
    {
        if (_routes.TryGetValue(vm, out var type))
        {
            await _navigationService.NavigateToTypeAsync(type);
            switch (vm)
            {
                case "CalibrationDetails":
                case "ValidationDetails":
                    CanSave = true;
                    CanPrint = true;
                    break;
                case "CalibrationHistory":
                case "ValidationHistory":
                case "Dashboard":
                    CanSave = false;
                    CanPrint = false;
                    break;
                case "Instruments":
                case "Standards":
                    CanSave = true;
                    CanPrint = false;
                    break;
            }
        }
        else
        {
            throw new KeyNotFoundException(vm);
        }
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        if (!CanSave || ViewModel is null)
            return;

        await ViewModel.Save();
    }

    [RelayCommand]
    private async Task PrintAsync()
    {
        if (!CanPrint || ViewModel is null)
            return;

        await ViewModel.Print();
    }

    [RelayCommand]
    private async Task RefreshAsync()
    {
        if (ViewModel is null)
            return;

        await ViewModel.Initialize();
    }
}
