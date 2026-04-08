// =============================================
// RpCosmos<T>.cs – Azure Cosmos DB repository
// =============================================
using System.Linq.Expressions;
using System.Net;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Linq;
using WaterOps.Repositories.Helpers;
using WaterOps.Repositories.Interfaces;
using WaterOps.Repositories.Models;
using WaterOps.Repositories.Secrets;
using WaterOps.Repositories.Services;

namespace WaterOps.Repositories.Services.Repositories;

/// <summary>
/// Azure Cosmos DB repository – cloud read/write with graceful error handling.
/// </summary>
public class RpCosmos<T> : IRepo<T>
    where T : class
{
    private readonly Container _container;

    public RpCosmos(CosmosClient client, DbContainer dbInformation)
    {
        _container = client.GetContainer(DbSecrets.Database, dbInformation.Container);
    }

    public async Task<DbBase<T>?> GetAsync(string id, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(id))
            throw new ArgumentException("Id cannot be null or empty.", nameof(id));

        try
        {
            var response = await _container.ReadItemAsync<DbBase<T>>(
                id,
                new PartitionKey(TypeKey.Of<T>()),
                cancellationToken: ct
            );
            return response.Resource;
        }
        catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }
        catch (Exception ex)
        {
            SyncLogger.Warn($"RpCosmos.GetAsync failed for {id}: {ex.GetType().Name}");
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
            using var iterator = _container
                .GetItemLinqQueryable<DbBase<T>>(
                    allowSynchronousQueryExecution: false,
                    requestOptions: new QueryRequestOptions
                    {
                        PartitionKey = new PartitionKey(TypeKey.Of<T>()),
                    }
                )
                .Where(predicate)
                .ToFeedIterator();

            var items = new List<DbBase<T>>();
            while (iterator.HasMoreResults)
                items.AddRange(await iterator.ReadNextAsync(ct));

            return items.OrderBy(i => i.Updated).ToList();
        }
        catch (Exception ex)
        {
            SyncLogger.Warn($"RpCosmos.GetWhereAsync failed: {ex.GetType().Name}");
            return [];
        }
    }

    public async Task<DbBase<T>?> PostAsync(DbBase<T> item, CancellationToken ct = default)
    {
        var finalId = string.IsNullOrWhiteSpace(item.Id) ? Guid.NewGuid().ToString() : item.Id;
        try
        {
            item = item with
            {
                Id = finalId,
                ObjId = null,
                PartitionKey = TypeKey.Of<T>(),
                IsSynced = true,
                Created = item.Created ?? DateTimeOffset.UtcNow,
                Updated = item.Updated ?? DateTimeOffset.UtcNow,
            };
            await _container.CreateItemAsync(
                item,
                new PartitionKey(item.PartitionKey),
                cancellationToken: ct
            );
            return item;
        }
        catch (Exception ex)
        {
            SyncLogger.Error($"RpCosmos.PostAsync failed for {finalId}", ex);
            return null;
        }
    }

    public async Task<DbBase<T>?> PutAsync(DbBase<T> item, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(item.Id))
            throw new ArgumentException("Item must have a valid Id for update.", nameof(item));

        try
        {
            item = item with { IsSynced = true, PartitionKey = TypeKey.Of<T>(), ObjId = null };
            await _container.UpsertItemAsync(
                item,
                new PartitionKey(item.PartitionKey),
                cancellationToken: ct
            );
            return item;
        }
        catch (Exception ex)
        {
            SyncLogger.Error($"RpCosmos.PutAsync failed for {item.Id}", ex);
            return null;
        }
    }

    public async Task<bool> DeleteAsync(DbBase<T> item, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(item.Id))
            throw new ArgumentException("Id cannot be null or empty.", nameof(item));

        try
        {
            var response = await _container.DeleteItemAsync<DbBase<T>>(
                item.Id,
                new PartitionKey(item.PartitionKey),
                cancellationToken: ct
            );
            return (int)response.StatusCode is >= 200 and <= 299;
        }
        catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            return true; // already deleted – treat as success
        }
        catch (Exception ex)
        {
            SyncLogger.Error($"RpCosmos.DeleteAsync failed for {item.Id}", ex);
            return false;
        }
    }
}
