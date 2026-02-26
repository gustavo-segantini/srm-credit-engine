using Microsoft.EntityFrameworkCore;
using SrmCreditEngine.Domain.Entities;
using SrmCreditEngine.Domain.Interfaces.Repositories;
using SrmCreditEngine.Infrastructure.Data;

namespace SrmCreditEngine.Infrastructure.Repositories;

public sealed class SettlementRepository : Repository<Settlement>, ISettlementRepository
{
    public SettlementRepository(AppDbContext dbContext) : base(dbContext) { }

    public async Task<Settlement?> GetByReceivableIdAsync(
        Guid receivableId,
        CancellationToken cancellationToken = default)
        => await DbSet
            .FirstOrDefaultAsync(s => s.ReceivableId == receivableId, cancellationToken);

    public async Task<Settlement?> GetForUpdateAsync(
        Guid settlementId,
        CancellationToken cancellationToken = default)
    {
        // For optimistic locking: load with tracking enabled
        // EF Core uses the xmin concurrency token configured in SettlementConfiguration
        return await DbSet
            .Where(s => s.Id == settlementId)
            .FirstOrDefaultAsync(cancellationToken);
    }
}
