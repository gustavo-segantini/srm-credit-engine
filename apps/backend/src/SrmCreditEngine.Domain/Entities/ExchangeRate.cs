using SrmCreditEngine.Domain.Enums;
using SrmCreditEngine.Domain.Exceptions;

namespace SrmCreditEngine.Domain.Entities;

/// <summary>
/// Stores the exchange rate between two currencies at a point in time.
/// Uses optimistic concurrency (RowVersion) to prevent race conditions on updates.
/// </summary>
public sealed class ExchangeRate : Entity
{
    public Guid FromCurrencyId { get; private set; }
    public Guid ToCurrencyId { get; private set; }
    public decimal Rate { get; private set; }
    public DateTime EffectiveDate { get; private set; }
    public DateTime? ExpiresAt { get; private set; }
    public string Source { get; private set; }

    /// <summary>Optimistic concurrency token — EF Core maps to xmin (PostgreSQL) or rowversion.</summary>
    public uint RowVersion { get; private set; }

    // Navigation
    public Currency FromCurrency { get; private set; } = null!;
    public Currency ToCurrency { get; private set; } = null!;

    private ExchangeRate() { }  // EF Core

    public ExchangeRate(
        Guid fromCurrencyId,
        Guid toCurrencyId,
        decimal rate,
        DateTime effectiveDate,
        string source,
        DateTime? expiresAt = null)
    {
        if (rate <= 0)
            throw new InvalidPricingException("Exchange rate must be greater than zero.");

        FromCurrencyId = fromCurrencyId;
        ToCurrencyId = toCurrencyId;
        Rate = rate;
        EffectiveDate = effectiveDate;
        ExpiresAt = expiresAt;
        Source = source;
    }

    public void Update(decimal newRate, string source)
    {
        if (newRate <= 0)
            throw new InvalidPricingException("Exchange rate must be greater than zero.");

        Rate = newRate;
        Source = source;
        EffectiveDate = DateTime.UtcNow;
        TouchUpdatedAt();
    }

    public bool IsValid(DateTime atDate) =>
        EffectiveDate <= atDate && (ExpiresAt == null || ExpiresAt > atDate);
}
