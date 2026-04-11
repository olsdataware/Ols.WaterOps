using LiteDB;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.DependencyInjection;
using WaterOps.Repositories.Helpers;
using WaterOps.Repositories.Interfaces;
using WaterOps.Repositories.Models;
using WaterOps.Repositories.Secrets;
using WaterOps.Repositories.Services.Repositories;

namespace WaterOps.Repositories.Services.Extensions;

public static class Inject
{
    public static IServiceCollection AddRepositories(
        this IServiceCollection collection,
        DbContainer dbContainer
    )
    {
        collection.AddScoped(_ => new LiteDatabase(
            $"Filename={Path.Combine(PathHelper.BasePath,
            $"{dbContainer.Container}.db")};Connection=shared;"
        ));

        collection.AddScoped(_ => new CosmosClient(
            DbSecrets.Endpoint,
            DbSecrets.Key,
            new CosmosClientOptions() { ConnectionMode = ConnectionMode.Gateway }
        ));

        collection.AddScoped(_ => dbContainer);

        collection.AddScoped(typeof(RpLite<>));
        collection.AddScoped(typeof(IRepo<>), typeof(RpCosmos<>));
        collection.AddScoped(typeof(ISyncService<>), typeof(SyncService<>));

        return collection;
    }
}
