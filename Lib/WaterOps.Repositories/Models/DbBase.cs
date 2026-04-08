namespace WaterOps.Repositories.Models;

using LiteDB;
using Newtonsoft.Json;
using WaterOps.Core.Models;

public record class DbBase<T>
    where T : class
{
    // ======= CosmosDB Properties =======
    [JsonProperty("id")]
    public string? Id { get; init; }

    [JsonProperty("PartitionKey")]
    public string? PartitionKey { get; init; }

    // ======= LiteDB Properties =======
    [BsonId]
    public ObjectId? ObjId { get; init; }

    // ======= Common Properties =======
    public PwsInformation? PwsInformation { get; init; }
    public DateTimeOffset? Created { get; init; }
    public DateTimeOffset? Updated { get; init; }
    public bool IsSynced { get; init; }
    public bool IsDeleted { get; init; }
    public T? Data { get; init; }
}
