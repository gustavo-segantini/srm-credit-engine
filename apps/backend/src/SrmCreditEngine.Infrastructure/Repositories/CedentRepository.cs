using Microsoft.EntityFrameworkCore;
using SrmCreditEngine.Domain.Entities;
using SrmCreditEngine.Domain.Interfaces.Repositories;
using SrmCreditEngine.Infrastructure.Data;

namespace SrmCreditEngine.Infrastructure.Repositories;

public sealed class CedentRepository : Repository<Cedent>, ICedentRepository
{
    public CedentRepository(AppDbContext dbContext) : base(dbContext) { }

    public async Task<Cedent?> GetByCnpjAsync(string cnpj, CancellationToken cancellationToken = default)
        => await DbSet.FirstOrDefaultAsync(c => c.Cnpj == cnpj && c.IsActive, cancellationToken);

    public async Task<bool> ExistsByCnpjAsync(string cnpj, CancellationToken cancellationToken = default)
        => await DbSet.AnyAsync(c => c.Cnpj == cnpj, cancellationToken);

    public async Task<IReadOnlyList<Cedent>> GetAllActiveAsync(CancellationToken cancellationToken = default)
        => await DbSet.Where(c => c.IsActive).OrderBy(c => c.Name).ToListAsync(cancellationToken);
}
