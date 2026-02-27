using Microsoft.EntityFrameworkCore;
using SrmCreditEngine.Domain.Entities;
using SrmCreditEngine.Domain.Interfaces.Repositories;
using SrmCreditEngine.Infrastructure.Data;

namespace SrmCreditEngine.Infrastructure.Repositories;

public sealed class ReceivableRepository : Repository<Receivable>, IReceivableRepository
{
    public ReceivableRepository(AppDbContext dbContext) : base(dbContext) { }

    public async Task<IReadOnlyList<Receivable>> GetByCedentAsync(
        Guid cedentId,
        CancellationToken cancellationToken = default)
        => await DbSet
            .Where(r => r.CedentId == cedentId)
            .OrderByDescending(r => r.SubmittedAt)
            .ToListAsync(cancellationToken);

    public async Task<Receivable?> GetByDocumentNumberAsync(
        string documentNumber,
        Guid cedentId,
        CancellationToken cancellationToken = default)
        => await DbSet
            .FirstOrDefaultAsync(
                r => r.DocumentNumber == documentNumber && r.CedentId == cedentId,
                cancellationToken);

    public async Task<bool> HasSettlementAsync(
        Guid receivableId,
        CancellationToken cancellationToken = default)
        => await DbContext.Set<Settlement>()
            .AnyAsync(s => s.ReceivableId == receivableId, cancellationToken);
}
