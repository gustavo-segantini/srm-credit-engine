using SrmCreditEngine.Domain.Enums;
using SrmCreditEngine.Domain.Interfaces.Strategies;
using SrmCreditEngine.Domain.ValueObjects;

namespace SrmCreditEngine.Application.Strategies;

/// <summary>
/// Pricing strategy for Cheque Pré-datado.
/// Higher risk than duplicata — Spread: 2.5% a.m. (monthly)
/// Formula: PV = FaceValue / (1 + BaseRate + 0.025)^TermInMonths
/// </summary>
public sealed class ChequePredatadoPricingStrategy : IPricingStrategy
{
    private const decimal SpreadMonthly = 0.025m; // 2.5% a.m.

    public ReceivableType SupportedType => ReceivableType.ChequePredatado;

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
            netDisbursement: presentValue,
            exchangeRateApplied: 1m
        );
    }
}
