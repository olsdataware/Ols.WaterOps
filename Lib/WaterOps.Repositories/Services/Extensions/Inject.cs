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
    public static void AddRepositories(this IServiceCollection services, DbContainer dbContainer)
    {
        services.AddScoped(_ => new LiteDatabase(
            $"Filename={Path.Combine(PathHelper.BasePath, 
            $"{dbContainer.Container}.db")};Connection=shared;"
        ));

        services.AddScoped(_ => new CosmosClient(
            DbSecrets.Endpoint,
            DbSecrets.Key,
            new CosmosClientOptions() { ConnectionMode = ConnectionMode.Gateway }
        ));

        services.AddScoped(_ => dbContainer);

        services.AddScoped(typeof(RpLite<>));
        services.AddScoped(typeof(IRepo<>), typeof(RpCosmos<>));
        services.AddScoped(typeof(ISyncService<>), typeof(SyncService<>));
    }
}
