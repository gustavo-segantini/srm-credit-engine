using Microsoft.EntityFrameworkCore;
using SrmCreditEngine.Domain.Entities;
using SrmCreditEngine.Domain.Enums;
using SrmCreditEngine.Domain.Interfaces.Repositories;
using SrmCreditEngine.Infrastructure.Data;

namespace SrmCreditEngine.Infrastructure.Repositories;

public sealed class ExchangeRateRepository : Repository<ExchangeRate>, IExchangeRateRepository
{
    public ExchangeRateRepository(AppDbContext dbContext) : base(dbContext) { }

    public async Task<ExchangeRate?> GetLatestAsync(
        CurrencyCode fromCurrency,
        CurrencyCode toCurrency,
        DateTime? atDate = null,
        CancellationToken cancellationToken = default)
    {
        var date = atDate ?? DateTime.UtcNow;

        return await DbSet
            .Include(e => e.FromCurrency)
            .Include(e => e.ToCurrency)
            .Where(e =>
                e.FromCurrency.Code == fromCurrency &&
                e.ToCurrency.Code == toCurrency &&
                e.EffectiveDate <= date &&
                (e.ExpiresAt == null || e.ExpiresAt > date))
            .OrderByDescending(e => e.EffectiveDate)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ExchangeRate>> GetHistoryAsync(
        CurrencyCode fromCurrency,
        CurrencyCode toCurrency,
        DateTime from,
        DateTime to,
        CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Include(e => e.FromCurrency)
            .Include(e => e.ToCurrency)
            .Where(e =>
                e.FromCurrency.Code == fromCurrency &&
                e.ToCurrency.Code == toCurrency &&
                e.EffectiveDate >= from &&
                e.EffectiveDate <= to)
            .OrderByDescending(e => e.EffectiveDate)
            .ToListAsync(cancellationToken);
    }
}
