using SrmCreditEngine.Domain.Enums;
using SrmCreditEngine.Domain.Exceptions;

namespace SrmCreditEngine.Domain.ValueObjects;

/// <summary>
/// Immutable value object representing a monetary amount in a specific currency.
/// Uses decimal for financial precision (no floating-point rounding errors).
/// </summary>
public sealed record Money
{
    public decimal Amount { get; }
    public CurrencyCode Currency { get; }

    public Money(decimal amount, CurrencyCode currency)
    {
        if (amount < 0)
            throw new InvalidPricingException("Money amount cannot be negative.");

        Amount = Math.Round(amount, 8, MidpointRounding.AwayFromZero);
        Currency = currency;
    }

    public Money Add(Money other)
    {
        EnsureSameCurrency(other);
        return new Money(Amount + other.Amount, Currency);
    }

    public Money Subtract(Money other)
    {
        EnsureSameCurrency(other);
        return new Money(Amount - other.Amount, Currency);
    }

    public Money Multiply(decimal factor)
        => new(Amount * factor, Currency);

    public Money Divide(decimal divisor)
    {
        if (divisor == 0)
            throw new InvalidPricingException("Division by zero in money calculation.");
        return new Money(Amount / divisor, Currency);
    }

    public Money ConvertTo(CurrencyCode targetCurrency, decimal exchangeRate)
    {
        if (exchangeRate <= 0)
            throw new InvalidPricingException("Exchange rate must be positive.");
        return new Money(Amount * exchangeRate, targetCurrency);
    }

    /// <summary>Returns amount rounded to 2 decimal places for display/settlement.</summary>
    public decimal ToSettlementAmount()
        => Math.Round(Amount, 2, MidpointRounding.AwayFromZero);

    private void EnsureSameCurrency(Money other)
    {
        if (Currency != other.Currency)
            throw new InvalidPricingException(
                $"Cannot operate on different currencies: {Currency} and {other.Currency}.");
    }

    public override string ToString() => $"{Amount:F8} {Currency}";
}
