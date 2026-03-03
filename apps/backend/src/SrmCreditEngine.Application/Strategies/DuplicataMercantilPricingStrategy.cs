using SrmCreditEngine.Domain.Enums;
using SrmCreditEngine.Domain.Interfaces.Strategies;
using SrmCreditEngine.Domain.ValueObjects;

namespace SrmCreditEngine.Application.Strategies;

/// <summary>
/// Pricing strategy for Duplicata Mercantil.
/// Spread: 1.5% a.m. (monthly)
/// Formula: PV = FaceValue / (1 + BaseRate + 0.015)^TermInMonths
/// </summary>
public sealed class DuplicataMercantilPricingStrategy : IPricingStrategy
{
    private const decimal SpreadMonthly = 0.015m; // 1.5% a.m.

    public ReceivableType SupportedType => ReceivableType.DuplicataMercantil;

    public PricingResult Calculate(Money faceValue, int termInMonths, decimal baseRate)
    {
        var totalRate = 1m + baseRate + SpreadMonthly;
        var compoundFactor = (decimal)Math.Pow((double)totalRate, termInMonths);
        var presentValue = faceValue.Divide(compoundFactor);

        return new PricingResult(
            faceValue: faceValue,
            presentValue: presentValue,
            appliedSpread: SpreadMonthly,
            baseRate: baseRate,
            termInMonths: termInMonths,
            netDisbursement: presentValue,   // same currency — cross-currency applied upstream
            exchangeRateApplied: 1m
        );
    }
}
