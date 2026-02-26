using SrmCreditEngine.Domain.Enums;

namespace SrmCreditEngine.Domain.ValueObjects;

/// <summary>
/// Encapsulates the result of the pricing engine calculation.
/// Immutable — once a receivable is priced, the result is sealed.
/// </summary>
public sealed record PricingResult
{
    /// <summary>Original face value of the receivable.</summary>
    public Money FaceValue { get; }

    /// <summary>
    /// Present value = FaceValue / (1 + BaseRate + Spread)^Term
    /// Calculated in the receivable's original currency.
    /// </summary>
    public Money PresentValue { get; }

    /// <summary>Discount amount (deságio) = FaceValue - PresentValue.</summary>
    public Money Discount { get; }

    /// <summary>Effective discount rate applied.</summary>
    public decimal AppliedSpread { get; }

    /// <summary>Base rate used in calculation.</summary>
    public decimal BaseRate { get; }

    /// <summary>Term in months used in calculation.</summary>
    public int TermInMonths { get; }

    /// <summary>
    /// Net disbursement value in the payment currency (may differ if cross-currency).
    /// </summary>
    public Money NetDisbursement { get; }

    /// <summary>Exchange rate applied (1.0 if same currency).</summary>
    public decimal ExchangeRateApplied { get; }

    public bool IsCrossCurrency => FaceValue.Currency != NetDisbursement.Currency;

    public PricingResult(
        Money faceValue,
        Money presentValue,
        decimal appliedSpread,
        decimal baseRate,
        int termInMonths,
        Money netDisbursement,
        decimal exchangeRateApplied)
    {
        FaceValue = faceValue;
        PresentValue = presentValue;
        Discount = faceValue.Subtract(presentValue);
        AppliedSpread = appliedSpread;
        BaseRate = baseRate;
        TermInMonths = termInMonths;
        NetDisbursement = netDisbursement;
        ExchangeRateApplied = exchangeRateApplied;
    }
}
