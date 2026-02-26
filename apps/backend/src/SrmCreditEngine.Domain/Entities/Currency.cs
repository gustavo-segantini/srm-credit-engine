using SrmCreditEngine.Domain.Enums;

namespace SrmCreditEngine.Domain.Entities;

/// <summary>
/// Represents a currency supported by the engine (BRL, USD).
/// </summary>
public sealed class Currency : Entity
{
    public CurrencyCode Code { get; private set; }
    public string Name { get; private set; }
    public string Symbol { get; private set; }
    public int DecimalPlaces { get; private set; }
    public bool IsActive { get; private set; }

    // Navigation
    public IReadOnlyCollection<ExchangeRate> ExchangeRatesFrom { get; private set; } = new List<ExchangeRate>();
    public IReadOnlyCollection<ExchangeRate> ExchangeRatesTo { get; private set; } = new List<ExchangeRate>();

    private Currency() { }  // EF Core

    public Currency(CurrencyCode code, string name, string symbol, int decimalPlaces = 2)
    {
        Code = code;
        Name = name;
        Symbol = symbol;
        DecimalPlaces = decimalPlaces;
        IsActive = true;
    }

    public void Deactivate()
    {
        IsActive = false;
        TouchUpdatedAt();
    }
}
