using Microsoft.EntityFrameworkCore;
using SrmCreditEngine.Domain.Entities;
using SrmCreditEngine.Domain.Enums;
using SrmCreditEngine.Domain.Interfaces.Repositories;
using SrmCreditEngine.Infrastructure.Data;

namespace SrmCreditEngine.Infrastructure.Repositories;

public sealed class CurrencyRepository(AppDbContext dbContext) : Repository<Currency>(dbContext), ICurrencyRepository
{
    public async Task<Currency?> GetByCodeAsync(CurrencyCode code, CancellationToken cancellationToken = default)
        => await DbSet.FirstOrDefaultAsync(c => c.Code == code && c.IsActive, cancellationToken);

    public async Task<IReadOnlyList<Currency>> GetAllActiveAsync(CancellationToken cancellationToken = default)
        => await DbSet.Where(c => c.IsActive).ToListAsync(cancellationToken);
}
