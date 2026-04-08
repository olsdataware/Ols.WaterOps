namespace WaterOps.Repositories.Interfaces;

using System.Linq.Expressions;
using WaterOps.Repositories.Models;

public interface ISyncService<T>
    where T : class
{
    // --- UI CRUD Operations (Always Local) ---
    Task<DbBase<T>?> GetAsync(string id, CancellationToken ct = default);
    Task<IEnumerable<DbBase<T>>> GetWhereAsync(
        Expression<Func<DbBase<T>, bool>> predicate,
        CancellationToken ct = default
    );
    Task<DbBase<T>?> SaveAsync(DbBase<T> item, CancellationToken ct = default);
    Task<bool> DeleteAsync(DbBase<T> item, CancellationToken ct = default);

    // --- Sync Management ---
    int PendingSyncCount { get; }

    // False until the first remote pull completes (success or failure).
    // ViewModels can bind to this to show a non-blocking "Syncing..." indicator
    // while still rendering whatever local data is already available.
    bool IsInitialSyncComplete { get; }

    // Call this from a "Refresh" button or during ViewModel initialization
    Task PullRemoteUpdatesAsync(CancellationToken ct = default);
}
