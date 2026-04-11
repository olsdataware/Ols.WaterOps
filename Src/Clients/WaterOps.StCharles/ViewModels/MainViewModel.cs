using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using WaterOps.Calibrations.ViewModels;
using WaterOps.Core.Models;
using WaterOps.Resources.Controls.ViewModels;
using WaterOps.Resources.Controls.Views;
using WaterOps.Resources.Enums;
using WaterOps.Resources.Interfaces;
using WaterOps.Updates.Interfaces;

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
    private readonly IUpdateService _updateService;
    private readonly IDialogService _dialogService;
    private string? _pendingUpdateVersion;
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

    public MainViewModel(
        INavigationService navigationService,
        IUpdateService updateService,
        IDialogService dialogService
    )
    {
        _navigationService = navigationService;
        _navigationService.ViewModelChanged += OnViewModelChanged;

        _updateService = updateService;
        _dialogService = dialogService;
        _updateService.UpdateAvailable += (_, version) =>
            Dispatcher.UIThread.Post(async () => await ShowUpdatePromptAsync(version));
    }

    public async Task InitializeAsync()
    {
        Pws = PwsList[0];
        _updateService.Start();

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

    /// <summary>
    /// Shows the update prompt. Yes restarts immediately; Later dismisses so the
    /// user can install via the manual check button whenever they are ready.
    /// </summary>
    private async Task ShowUpdatePromptAsync(string version)
    {
        _pendingUpdateVersion = version;

        var vm = new UpdatePromptViewModel { Version = version };
        var view = new UpdatePromptView();

        var result = await _dialogService.ShowAsync(view, vm);
        if (result == DialogResult.Yes)
            _updateService.RestartAndApply();
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

    [RelayCommand]
    private async Task CheckUpdatesAsync()
    {
        if (_updateService.IsUpdatePending && _pendingUpdateVersion is not null)
        {
            await ShowUpdatePromptAsync(_pendingUpdateVersion);
            return;
        }

        await _updateService.CheckForUpdatesAsync();
    }
}
