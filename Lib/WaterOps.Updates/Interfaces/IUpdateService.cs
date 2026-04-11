namespace WaterOps.Updates.Interfaces;

public interface IUpdateService : IDisposable
{
    event EventHandler<string>? UpdateAvailable;
    bool IsUpdatePending { get; }
    void Start();
    Task<bool> CheckForUpdatesAsync();
    void RestartAndApply();
}
