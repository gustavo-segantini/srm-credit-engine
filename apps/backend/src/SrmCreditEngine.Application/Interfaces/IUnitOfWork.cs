namespace SrmCreditEngine.Application.Interfaces;

/// <summary>
/// Unit of Work pattern — coordinates ACID transactions across multiple repositories.
/// Ensures that all repository operations within a single business action either commit or rollback together.
/// </summary>
public interface IUnitOfWork : IDisposable
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes the given action within a database transaction.
    /// Rolls back automatically on exception.
    /// </summary>
    Task ExecuteInTransactionAsync(Func<Task> action, CancellationToken cancellationToken = default);
}
