using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using SrmCreditEngine.Application.Interfaces;
using SrmCreditEngine.Infrastructure.Data;

namespace SrmCreditEngine.Infrastructure;

/// <summary>
/// Coordinates ACID transactions. Uses EF Core's built-in transaction management.
/// DbContext tracks all changes and flushes them within the same transaction.
/// </summary>
public sealed class UnitOfWork(AppDbContext dbContext) : IUnitOfWork
{
    private IDbContextTransaction? _currentTransaction;

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        => await dbContext.SaveChangesAsync(cancellationToken);

    public async Task ExecuteInTransactionAsync(
        Func<Task> action,
        CancellationToken cancellationToken = default)
    {
        // If there's already an active transaction (nested call), just execute the action
        if (dbContext.Database.CurrentTransaction != null)
        {
            await action();
            return;
        }

        // NpgsqlRetryingExecutionStrategy requires all user-initiated transactions
        // to be wrapped with CreateExecutionStrategy so retries work correctly.
        var strategy = dbContext.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            _currentTransaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                await action();
                await _currentTransaction.CommitAsync(cancellationToken);
            }
            catch
            {
                await _currentTransaction.RollbackAsync(cancellationToken);
                throw;
            }
            finally
            {
                await _currentTransaction.DisposeAsync();
                _currentTransaction = null;
            }
        });
    }

    public void Dispose()
    {
        _currentTransaction?.Dispose();
        dbContext.Dispose();
    }
}
