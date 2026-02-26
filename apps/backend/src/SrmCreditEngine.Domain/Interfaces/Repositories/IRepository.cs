namespace SrmCreditEngine.Domain.Interfaces.Repositories;

/// <summary>Base repository contract for all aggregates.</summary>
public interface IRepository<T> where T : class
{
    Task<T?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task AddAsync(T entity, CancellationToken cancellationToken = default);
    void Update(T entity);
    void Remove(T entity);
}
