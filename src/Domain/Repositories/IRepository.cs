namespace AfriPay.Domain.Repositories;

 
/// <summary>
/// Interface générique de repository.
/// Définit les opérations CRUD communes à tous les agrégats.
/// Ne dépend d'aucun framework — pas de DbSet, pas de IQueryable.
/// </summary>
public interface IRepository<T> where T : class
{
    Task<T?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task     AddAsync(T entity, CancellationToken ct = default);
    void     Update(T entity);
    void     Delete(T entity);
}