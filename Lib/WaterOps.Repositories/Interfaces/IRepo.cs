namespace WaterOps.Repositories.Interfaces;

using System.Linq.Expressions;
using WaterOps.Repositories.Models;

public interface IRepo<T>
    where T : class
{
    public Task<DbBase<T>?> GetAsync(string id, CancellationToken ct = default);
    public Task<IEnumerable<DbBase<T>>> GetWhereAsync(
        Expression<Func<DbBase<T>, bool>> predicate,
        CancellationToken ct = default
    );
    public Task<DbBase<T>?> PostAsync(DbBase<T> item, CancellationToken ct = default);
    public Task<DbBase<T>?> PutAsync(DbBase<T> item, CancellationToken ct = default);
    public Task<bool> DeleteAsync(DbBase<T> item, CancellationToken ct = default);
}
