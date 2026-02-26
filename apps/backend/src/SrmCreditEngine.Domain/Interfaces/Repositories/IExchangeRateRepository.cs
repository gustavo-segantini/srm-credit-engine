using SrmCreditEngine.Domain.Entities;
using SrmCreditEngine.Domain.Enums;

namespace SrmCreditEngine.Domain.Interfaces.Repositories;

public interface IExchangeRateRepository : IRepository<ExchangeRate>
{
    /// <summary>
    /// Returns the most recent valid exchange rate for the given currency pair at a specific date.
    /// </summary>
    Task<ExchangeRate?> GetLatestAsync(
        CurrencyCode fromCurrency,
        CurrencyCode toCurrency,
        DateTime? atDate = null,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ExchangeRate>> GetHistoryAsync(
        CurrencyCode fromCurrency,
        CurrencyCode toCurrency,
        DateTime from,
        DateTime to,
        CancellationToken cancellationToken = default);
}
