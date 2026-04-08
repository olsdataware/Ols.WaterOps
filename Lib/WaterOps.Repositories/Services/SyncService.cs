// =============================================
// SyncService<T>.cs – Offline-first coordinator
// =============================================
using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Threading.Channels;
using WaterOps.Repositories.Interfaces;
using WaterOps.Repositories.Models;
using WaterOps.Repositories.Services.Repositories;

namespace WaterOps.Repositories.Services;

/// <summary>
/// Offline-first repository coordinator for a hub-and-spoke topology:
/// one master facility writes bulk data; remote machines read and occasionally write.
///
/// READ
///   GetAsync     – Returns local immediately if data is fresh (last pull &lt; 2 min) or has
///                  uncommitted local changes. Otherwise races LiteDB vs Cosmos (3-second cap)
///                  and caches the winner locally. Returns null for soft-deleted items.
///   GetWhereAsync – Returns local only. Callers MUST include !x.IsDeleted in the predicate
///                  to exclude soft-deleted tombstones. The 15-minute pull loop keeps the
///                  cache current with changes made at the master facility.
///
/// WRITE
///   SaveAsync  – Writes to LiteDB instantly (IsSynced=false), enqueues id for Cosmos push.
///   DeleteAsync – Soft-deletes locally (IsDeleted=true, IsSynced=false), enqueues for push.
///                  The push worker upserts the tombstone to Cosmos so ALL other devices see
///                  the deletion on their next pull. No hard-deletes in Cosmos.
///
/// SYNC
///   • Background pull on startup + every 15 minutes (delta: only items updated since last sync).
///   • Pull propagates tombstones (IsDeleted=true) to all devices automatically.
///   • Unsynced items are pushed on startup and on every pull cycle as a backstop.
///   • Failed pushes retry after 60 seconds (non-blocking). Items survive restarts via IsSynced=false.
/// </summary>
public class SyncService<T> : ISyncService<T>, IDisposable, IAsyncDisposable
    where T : class
{
    private readonly RpLite<T> _lite;
    private readonly IRepo<T> _cosmos;

    // Id-based channel: push worker always reads the freshest version from LiteDB.
    private readonly Channel<string> _pushChannel;

    // Tracks ids currently queued or being retried to prevent duplicate enqueues.
    private readonly ConcurrentDictionary<string, byte> _pendingIds = new();

    private readonly PeriodicTimer _pullTimer;
    private readonly CancellationTokenSource _cts = new();
    private readonly Task _pushTask;
    private readonly Task _pullTask;

    private readonly TaskCompletionSource _initialPullReady = new(
        TaskCreationOptions.RunContinuationsAsynchronously
    );
    private long _lastSyncTimeTicks = DateTimeOffset.MinValue.Ticks;
    private volatile bool _disposed;

    public SyncService(RpLite<T> lite, IRepo<T> cosmos)
        : this(lite, cosmos, startWorkers: true) { }

    internal SyncService(RpLite<T> lite, IRepo<T> cosmos, bool startWorkers)
    {
        _lite = lite;
        _cosmos = cosmos;
        _pushChannel = Channel.CreateUnbounded<string>(
            new UnboundedChannelOptions { SingleWriter = false, SingleReader = true }
        );
        _pullTimer = new PeriodicTimer(TimeSpan.FromMinutes(15));

        if (startWorkers)
        {
            _pushTask = Task.Run(() => ProcessPushQueueAsync(_cts.Token));
            _pullTask = Task.Run(() => ProcessPullLoopAsync(_cts.Token));
        }
        else
        {
            _initialPullReady.TrySetResult();
            _pushTask = Task.CompletedTask;
            _pullTask = Task.CompletedTask;
        }
    }

    // =============================================
    // ISyncService properties
    // =============================================

    public int PendingSyncCount => _pushChannel.Reader.Count;
    public bool IsInitialSyncComplete => _initialPullReady.Task.IsCompleted;

    // =============================================
    // Public CRUD (always local-first)
    // =============================================

    /// <summary>
    /// Returns the most current version of an item.
    ///
    /// Fast path (returns local immediately):
    ///   • Local item has IsSynced=false (uncommitted local changes are the truth).
    ///   • Last background pull completed within the past 2 minutes (cache is fresh).
    ///
    /// Freshness path (races LiteDB vs Cosmos with a 3-second cap):
    ///   • Local is missing or could be stale. If Cosmos wins, the local cache is updated
    ///     before returning so the caller always gets a fully-hydrated record (ObjId set).
    ///
    /// Returns null if the item does not exist or has been soft-deleted (IsDeleted=true).
    /// </summary>
    public async Task<DbBase<T>?> GetAsync(string id, CancellationToken ct = default)
    {
        var local = await _lite.GetAsync(id, ct);

        // Fast path: local changes are uncommitted (we are the authority) or data is fresh.
        if (local?.IsSynced == false || PullIsRecent())
            return local?.IsDeleted == true ? null : local;

        // Freshness path: check Cosmos for a newer version.
        var remote = await FetchFromCosmosAsync(id, ct);

        if (remote is not null && (local is null || remote.Updated > local.Updated))
        {
            // Remote is newer – cache locally (resolves ObjId) and return.
            var cached = await _lite.UpsertSilentAsync(remote with { IsSynced = true }, ct);
            var winner = cached ?? remote;
            return winner.IsDeleted ? null : winner;
        }

        return local?.IsDeleted == true ? null : local;
    }

    /// <summary>
    /// Returns all matching items from the local cache. Waits up to 5 seconds for the
    /// initial pull to complete before serving, then falls back to whatever is cached.
    ///
    /// IMPORTANT: always include !x.IsDeleted in your predicate to exclude soft-deleted
    /// tombstones that have not yet been cleaned up from the local store.
    ///
    /// Cosmos freshness is maintained by the 15-minute background pull loop.
    /// </summary>
    public async Task<IEnumerable<DbBase<T>>> GetWhereAsync(
        Expression<Func<DbBase<T>, bool>> predicate,
        CancellationToken ct = default
    )
    {
        try
        {
            await _initialPullReady.Task.WaitAsync(TimeSpan.FromSeconds(5), ct);
        }
        catch (TimeoutException)
        {
            SyncLogger.Warn(
                $"[{typeof(T).Name}] Initial sync not yet complete – serving local cache."
            );
        }

        return await _lite.GetWhereAsync(predicate, ct);
    }

    /// <summary>
    /// Writes to LiteDB immediately (IsSynced=false), then enqueues a background Cosmos push.
    /// Handles new items (no Id), existing local items (has ObjId), and Cosmos-sourced items
    /// (has Id but no ObjId – resolved internally).
    /// </summary>
    public async Task<DbBase<T>?> SaveAsync(DbBase<T> item, CancellationToken ct = default)
    {
        try
        {
            DbBase<T>? result;

            if (string.IsNullOrWhiteSpace(item.Id))
            {
                // New record.
                result = await _lite.PostAsync(item, ct);
            }
            else if (item.ObjId is not null)
            {
                // Existing local record – standard update.
                result = await _lite.PutAsync(item, ct);
            }
            else
            {
                // Item has a Cosmos Id but no LiteDB ObjId (e.g. returned by GetAsync when
                // Cosmos was newer, or constructed from a DTO). Resolve ObjId internally.
                result = await _lite.UpsertSilentAsync(item with { IsSynced = false }, ct);
            }

            if (result?.Id is not null)
            {
                EnqueuePush(result.Id);
                SyncLogger.Info($"[{typeof(T).Name}] Saved {result.Id} locally, queued for push.");
            }

            return result;
        }
        catch (Exception ex)
        {
            SyncLogger.Error($"[{typeof(T).Name}] SaveAsync failed", ex);
            return null;
        }
    }

    /// <summary>
    /// Soft-deletes locally (IsDeleted=true, IsSynced=false) and enqueues a Cosmos push.
    ///
    /// The push worker upserts a tombstone (IsDeleted=true) to Cosmos rather than hard-deleting.
    /// This ensures every remote machine sees the deletion on its next pull and updates its own
    /// local cache. No data is permanently removed from Cosmos by this library.
    /// </summary>
    public async Task<bool> DeleteAsync(DbBase<T> item, CancellationToken ct = default)
    {
        try
        {
            var tombstone = item with
            {
                IsDeleted = true,
                IsSynced = false,
                Updated = DateTimeOffset.UtcNow,
            };

            // Use UpsertSilentAsync as a fallback when ObjId is missing (item sourced
            // directly from Cosmos without a local fetch first).
            var result = item.ObjId is not null
                ? await _lite.PutAsync(tombstone, ct)
                : await _lite.UpsertSilentAsync(tombstone, ct);

            if (result?.Id is not null)
            {
                EnqueuePush(result.Id);
                SyncLogger.Info(
                    $"[{typeof(T).Name}] Soft-deleted {result.Id}, queued for tombstone push."
                );
            }

            return result is not null;
        }
        catch (Exception ex)
        {
            SyncLogger.Error($"[{typeof(T).Name}] DeleteAsync failed", ex);
            return false;
        }
    }

    // =============================================
    // Sync management
    // =============================================

    /// <summary>
    /// Pulls all items updated since the last sync from Cosmos and saves newer versions locally.
    /// Tombstones (IsDeleted=true) are included so deletions propagate to every device.
    /// </summary>
    public async Task PullRemoteUpdatesAsync(CancellationToken ct = default)
    {
        try
        {
            var lastSync = new DateTimeOffset(
                Interlocked.Read(ref _lastSyncTimeTicks),
                TimeSpan.Zero
            );
            var queryTime = DateTimeOffset.UtcNow;

            var remoteUpdates = (
                await _cosmos.GetWhereAsync(x => x.Updated > lastSync, ct)
            ).ToList();

            if (remoteUpdates.Count == 0)
            {
                Interlocked.Exchange(ref _lastSyncTimeTicks, queryTime.Ticks);
                return;
            }

            var remoteIds = remoteUpdates.Select(r => r.Id!).ToHashSet();
            var localItems = (
                await _lite.GetWhereAsync(x => remoteIds.Contains(x.Id!), ct)
            ).ToDictionary(x => x.Id!);

            foreach (var remote in remoteUpdates)
            {
                if (ct.IsCancellationRequested)
                    break;

                localItems.TryGetValue(remote.Id!, out var local);

                // Accept remote if local is missing or remote is strictly newer.
                // Never overwrite a local item that has uncommitted changes (IsSynced=false),
                // except when the remote version is a tombstone from another device – in that
                // case the deletion wins (master authority over remote edits on deleted items).
                if (local is null || remote.Updated > local.Updated)
                {
                    if (local?.IsSynced == false && !remote.IsDeleted)
                    {
                        // Local has pending changes and remote is not a deletion – keep local.
                        SyncLogger.Info(
                            $"[{typeof(T).Name}] Skipping pull for {remote.Id} – local has uncommitted changes."
                        );
                        continue;
                    }

                    await _lite.UpsertSilentAsync(remote with { IsSynced = true }, ct);
                }
            }

            Interlocked.Exchange(ref _lastSyncTimeTicks, queryTime.Ticks);
            SyncLogger.Info(
                $"[{typeof(T).Name}] Pulled {remoteUpdates.Count} update(s) from Cosmos."
            );
        }
        catch (Exception ex)
        {
            SyncLogger.Warn(
                $"[{typeof(T).Name}] PullRemoteUpdatesAsync failed: {ex.GetType().Name} – {ex.Message}"
            );
        }
    }

    // =============================================
    // Background workers
    // =============================================

    private async Task ProcessPullLoopAsync(CancellationToken ct)
    {
        // Initial pull on startup – signal ready regardless of success or failure.
        try
        {
            await PullRemoteUpdatesAsync(ct);
        }
        catch (Exception ex)
        {
            SyncLogger.Error($"[{typeof(T).Name}] Initial pull failed", ex);
        }
        finally
        {
            _initialPullReady.TrySetResult();
        }

        // Push any items that were offline before this session.
        await PushPendingOfflineItemsAsync(ct);

        // Periodic sync loop.
        try
        {
            while (await _pullTimer.WaitForNextTickAsync(ct))
            {
                await PullRemoteUpdatesAsync(ct);
                await PushPendingOfflineItemsAsync(ct);
            }
        }
        catch (OperationCanceledException)
        { /* normal shutdown */
        }
    }

    private async Task ProcessPushQueueAsync(CancellationToken ct)
    {
        await foreach (var id in _pushChannel.Reader.ReadAllAsync(ct))
        {
            // Release the slot BEFORE the attempt so SaveAsync can re-enqueue
            // new edits that arrive while this push is in flight.
            _pendingIds.TryRemove(id, out _);

            try
            {
                var item = await _lite.GetAsync(id, ct);

                // Skip if gone or already confirmed synced.
                if (item is null || item.IsSynced)
                    continue;

                // Both saves and soft-deletes are upserted to Cosmos.
                // IsDeleted=true items become tombstones that remote devices pull and apply locally.
                // No hard-deletes are issued to Cosmos – deletion is always a data change, not a removal.
                var pushed = string.IsNullOrWhiteSpace(item.Id)
                    ? await _cosmos.PostAsync(item, ct).WaitAsync(TimeSpan.FromSeconds(10), ct)
                    : await _cosmos.PutAsync(item, ct).WaitAsync(TimeSpan.FromSeconds(10), ct);

                if (pushed is not null)
                    await _lite.UpsertSilentAsync(item with { IsSynced = true }, ct);

                SyncLogger.Info($"[{typeof(T).Name}] Pushed {id} to Cosmos.");
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                return; // app is shutting down
            }
            catch (Exception ex)
            {
                // Network error, timeout, or transient failure – retry after 60s.
                // The item stays IsSynced=false in LiteDB as a permanent backstop.
                SyncLogger.Warn(
                    $"[{typeof(T).Name}] Push failed for {id} ({ex.GetType().Name}). Retrying in 60s."
                );
                ScheduleRetry(id, ct);
            }
        }
    }

    /// <summary>
    /// Loads all unsynced local items and adds them to the push queue,
    /// skipping any ids that are already queued or being retried.
    /// </summary>
    private async Task PushPendingOfflineItemsAsync(CancellationToken ct)
    {
        try
        {
            var pending = await _lite.GetWhereAsync(x => !x.IsSynced, ct);
            foreach (var item in pending)
                if (item.Id is not null)
                    EnqueuePush(item.Id);
        }
        catch (Exception ex)
        {
            SyncLogger.Error($"[{typeof(T).Name}] PushPendingOfflineItemsAsync failed", ex);
        }
    }

    // =============================================
    // Helpers
    // =============================================

    private async Task<DbBase<T>?> FetchFromCosmosAsync(string id, CancellationToken ct)
    {
        try
        {
            return await _cosmos.GetAsync(id, ct).WaitAsync(TimeSpan.FromSeconds(3), ct);
        }
        catch
        {
            return null; // offline, timeout, or unavailable – graceful fallback to local
        }
    }

    /// <summary>True when the last background pull completed within the past 2 minutes.</summary>
    private bool PullIsRecent()
    {
        var lastPull = new DateTimeOffset(Interlocked.Read(ref _lastSyncTimeTicks), TimeSpan.Zero);
        return DateTimeOffset.UtcNow - lastPull < TimeSpan.FromMinutes(2);
    }

    /// <summary>Enqueues an id only if it is not already pending, preventing duplicates.</summary>
    private void EnqueuePush(string id)
    {
        if (_pendingIds.TryAdd(id, 0))
            _pushChannel.Writer.TryWrite(id);
    }

    /// <summary>Schedules a non-blocking push retry after a 60-second delay.</summary>
    private void ScheduleRetry(string id, CancellationToken appCt)
    {
        _ = Task.Delay(TimeSpan.FromSeconds(60), CancellationToken.None)
            .ContinueWith(
                _ =>
                {
                    if (!_disposed && !appCt.IsCancellationRequested)
                        EnqueuePush(id);
                },
                TaskScheduler.Default
            );
    }

    // =============================================
    // Disposal
    // =============================================

    public void Dispose()
    {
        if (_disposed)
            return;
        _disposed = true;
        _pushChannel.Writer.TryComplete();
        _cts.Cancel();
        _pullTimer.Dispose();
        _cts.Dispose();
        GC.SuppressFinalize(this);
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
            return;
        _disposed = true;
        _pushChannel.Writer.TryComplete();
        _cts.Cancel();
        try
        {
            await Task.WhenAll(_pushTask, _pullTask).WaitAsync(TimeSpan.FromSeconds(5));
        }
        catch { }
        _pullTimer.Dispose();
        _cts.Dispose();
        GC.SuppressFinalize(this);
    }
}
