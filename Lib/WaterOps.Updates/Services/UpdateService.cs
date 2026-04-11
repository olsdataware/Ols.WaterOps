using Velopack;
using Velopack.Sources;
using WaterOps.Updates.Interfaces;

namespace WaterOps.Updates.Services;

/// <summary>
/// Provides update discovery, download, and apply workflows using Velopack.
/// Thread-safe, idempotent disposal, and resilient background polling.
/// </summary>
public class UpdateService : IUpdateService, IAsyncDisposable
{
    private readonly UpdateManager _mgr;
    private readonly CancellationTokenSource _cts = new();
    private readonly SemaphoreSlim _checkGate = new(1, 1);

    private UpdateInfo? _pendingUpdate;
    private int _isStarted;
    private int _isDisposed;
    private volatile bool _isUpdatePending;
    private Task? _loopTask;

    private const string BaseUpdateUrl = "https://waterops.blob.core.windows.net/updates/";

    /// <summary>
    /// Raised on a background thread when an update is ready.
    /// UI consumers must marshal to the main thread.
    /// </summary>
    public event EventHandler<string>? UpdateAvailable;
    public bool IsUpdatePending => _isUpdatePending;

    public UpdateService(string clientName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(clientName);

        var fullUpdateUrl = $"{BaseUpdateUrl.TrimEnd('/')}/{clientName.Trim('/')}/";
        _mgr = new UpdateManager(new SimpleWebSource(fullUpdateUrl));
    }

    public void Start()
    {
        if (Interlocked.Exchange(ref _isStarted, 1) == 1 || Volatile.Read(ref _isDisposed) == 1)
            return;

        UpdateLogger.Info("UpdateService started.");
        _loopTask = RunUpdateLoopAsync(_cts.Token);
    }

    private async Task RunUpdateLoopAsync(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            try
            {
                await CheckForUpdatesInternalAsync(token).ConfigureAwait(false);
                await Task.Delay(TimeSpan.FromHours(1), token).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                break; // Clean exit on cancellation
            }
            catch (Exception ex)
            {
                UpdateLogger.Error("Unhandled exception in update loop. Backing off 5 minutes.", ex);
                try
                {
                    // Back-off delay that won't fault the loop if cancelled
                    await Task.Delay(TimeSpan.FromMinutes(5), token).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
        }
    }

    public async Task<bool> CheckForUpdatesAsync()
    {
        if (Volatile.Read(ref _isDisposed) == 1)
            return false;
        return await CheckForUpdatesInternalAsync(CancellationToken.None).ConfigureAwait(false);
    }

    private async Task<bool> CheckForUpdatesInternalAsync(CancellationToken token)
    {
        try
        {
            await _checkGate.WaitAsync(token).ConfigureAwait(false);
        }
        catch (ObjectDisposedException)
        {
            return false;
        }
        catch (OperationCanceledException)
        {
            return false;
        }

        try
        {
            if (IsUpdatePending || !_mgr.IsInstalled)
                return IsUpdatePending;

            var newVersion = await _mgr.CheckForUpdatesAsync().ConfigureAwait(false);
            if (newVersion == null)
            {
                UpdateLogger.Info("No updates available.");
                return false;
            }

            var version = newVersion.TargetFullRelease.Version.ToString();
            UpdateLogger.Info($"Update found: {version}. Downloading...");
            await _mgr.DownloadUpdatesAsync(newVersion).ConfigureAwait(false);
            UpdateLogger.Info($"Update {version} downloaded and ready to apply.");

            _pendingUpdate = newVersion;
            _isUpdatePending = true;

            RaiseUpdateAvailable(version);
            return true;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            UpdateLogger.Error("Failed to check or download update.", ex);
            return false;
        }
        finally
        {
            try
            {
                _checkGate.Release();
            }
            catch (ObjectDisposedException) { }
        }
    }

    private void RaiseUpdateAvailable(string version)
    {
        var handlers = UpdateAvailable;
        if (handlers == null)
            return;

        foreach (var subscriber in handlers.GetInvocationList())
        {
            try
            {
                ((EventHandler<string>)subscriber).Invoke(this, version);
            }
            catch (Exception ex)
            {
                UpdateLogger.Error("UpdateAvailable subscriber threw an exception.", ex);
            }
        }
    }

    public void RestartAndApply()
    {
        if (Volatile.Read(ref _isDisposed) == 1 || !IsUpdatePending || _pendingUpdate == null)
            return;

        UpdateLogger.Info($"Applying update {_pendingUpdate.TargetFullRelease.Version} and restarting.");
        try
        {
            _mgr.ApplyUpdatesAndRestart(_pendingUpdate.TargetFullRelease);
        }
        catch (Exception ex)
        {
            UpdateLogger.Error("Failed to apply update and restart.", ex);
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref _isDisposed, 1) == 1)
            return;

        _cts.Cancel();

        if (_loopTask != null)
        {
            try
            {
                // Observe the task to prevent unobserved exceptions
                await Task.WhenAny(_loopTask, Task.Delay(2000)).ConfigureAwait(false);
                if (_loopTask.IsFaulted && _loopTask.Exception != null)
                    UpdateLogger.Error("Update loop faulted during shutdown.", _loopTask.Exception.Flatten());
            }
            catch
            { /* Final shutdown silence */
            }
        }

        DisposeInternal();
        GC.SuppressFinalize(this);
    }

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _isDisposed, 1) == 1)
            return;
        _cts.Cancel();
        DisposeInternal();
        GC.SuppressFinalize(this);
    }

    private void DisposeInternal()
    {
        _checkGate.Dispose();
        _cts.Dispose();
        // UpdateManager in current Velopack versions does not implement IDisposable.
    }
}
