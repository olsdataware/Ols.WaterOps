// =============================================
// RpLite<T>.cs – Local LiteDB repository
// =============================================
using System.Linq.Expressions;
using LiteDB;
using WaterOps.Repositories.Helpers;
using WaterOps.Repositories.Interfaces;
using WaterOps.Repositories.Models;
using WaterOps.Repositories.Services;

namespace WaterOps.Repositories.Services.Repositories;

/// <summary>
/// LiteDB repository – writes are synchronous for instant UI response.
/// UpsertSilentAsync is used by the sync layer to cache remote items locally.
/// </summary>
public class RpLite<T> : IRepo<T>
    where T : class
{
    private readonly LiteDatabase _database;
    private readonly ILiteCollection<DbBase<T>> _collection;

    public RpLite(LiteDatabase database)
    {
        _database = database;
        _collection = _database.GetCollection<DbBase<T>>(TypeKey.Of<T>());
        _collection.EnsureIndex(x => x.Id);
        _collection.EnsureIndex(x => x.IsSynced);
        _collection.EnsureIndex(x => x.Updated);
    }

    public async Task<DbBase<T>?> GetAsync(string id, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(id))
            throw new ArgumentException("Id cannot be null or empty.", nameof(id));

        try
        {
            return await Task.Run(() => _collection.FindOne(x => x.Id == id), ct);
        }
        catch (Exception ex)
        {
            SyncLogger.Error($"RpLite.GetAsync failed for {id}", ex);
            return null;
        }
    }

    public async Task<IEnumerable<DbBase<T>>> GetWhereAsync(
        Expression<Func<DbBase<T>, bool>> predicate,
        CancellationToken ct = default
    )
    {
        if (predicate is null)
            throw new ArgumentNullException(nameof(predicate));

        try
        {
            return await Task.Run(() => _collection.Find(predicate).ToList(), ct);
        }
        catch (Exception ex)
        {
            SyncLogger.Error("RpLite.GetWhereAsync failed", ex);
            return [];
        }
    }

    public async Task<DbBase<T>?> PostAsync(DbBase<T> item, CancellationToken ct = default)
    {
        if (!string.IsNullOrWhiteSpace(item.Id) || item.ObjId is not null)
            throw new ArgumentException("New item must not have an Id or ObjId.", nameof(item));

        try
        {
            var now = DateTimeOffset.UtcNow;
            item = item with
            {
                Id = Guid.NewGuid().ToString(),
                ObjId = ObjectId.NewObjectId(),
                IsSynced = false,
                Created = now,
                Updated = now,
            };
            _collection.Insert(item);
            return item;
        }
        catch (Exception ex)
        {
            SyncLogger.Error("RpLite.PostAsync failed", ex);
            return null;
        }
    }

    public async Task<DbBase<T>?> PutAsync(DbBase<T> item, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(item.Id) || item.ObjId is null)
            throw new ArgumentException(
                "Item must have a valid Id and ObjId for update.",
                nameof(item)
            );

        try
        {
            item = item with { Updated = DateTimeOffset.UtcNow, IsSynced = false };
            return _collection.Update(item) ? item : null;
        }
        catch (Exception ex)
        {
            SyncLogger.Error($"RpLite.PutAsync failed for {item.Id}", ex);
            return null;
        }
    }

    /// <summary>
    /// Upserts a remote item locally, preserving the existing ObjId if the document already exists.
    /// Returns the saved item (with ObjId resolved) so callers always have a fully-hydrated record.
    /// </summary>
    internal async Task<DbBase<T>?> UpsertSilentAsync(
        DbBase<T> item,
        CancellationToken ct = default
    )
    {
        try
        {
            return await Task.Run(
                () =>
                {
                    var existing = _collection.FindOne(x => x.Id == item.Id);
                    if (existing is null)
                    {
                        var inserted = item with { ObjId = ObjectId.NewObjectId() };
                        _collection.Insert(inserted);
                        return inserted;
                    }

                    var updated = item with { ObjId = existing.ObjId };
                    _collection.Update(updated);
                    return updated;
                },
                ct
            );
        }
        catch (Exception ex)
        {
            SyncLogger.Error($"RpLite.UpsertSilentAsync failed for {item.Id}", ex);
            return null;
        }
    }

    public async Task<bool> DeleteAsync(DbBase<T> item, CancellationToken ct = default)
    {
        if (item.ObjId is null)
            throw new ArgumentException("ObjId cannot be null for hard delete.", nameof(item));

        try
        {
            return _collection.Delete(item.ObjId);
        }
        catch (Exception ex)
        {
            SyncLogger.Error($"RpLite.DeleteAsync failed for {item.Id}", ex);
            return false;
        }
    }
}
