using AfriPay.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace AfriPay.Infrastructure.Persistence.Repositories;

internal abstract class Repository<T>(AfriPayDbContextBase db) : IRepository<T> where T : class
{
    protected readonly AfriPayDbContextBase Db = db;
    protected DbSet<T> Set => Db.Set<T>();
    public async Task<T?> GetByIdAsync(Guid id, CancellationToken ct = default) => await Set.FindAsync([id], ct);
    public async Task AddAsync(T entity, CancellationToken ct = default) => await Set.AddAsync(entity, ct);
    public void Update(T entity) => Set.Update(entity);
    public void Delete(T entity) => Set.Remove(entity);
}