using SrmCreditEngine.Domain.Enums;
using SrmCreditEngine.Domain.Exceptions;
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
        // FIX (hotfix/fix-cheque-zero-term): termInMonths <= 0 caused silent pricing anomaly.
        // termInMonths == 0 → Math.Pow(rate,0) = 1 → PV == FaceValue (zero discount applied).
        // termInMonths < 0  → PV > FaceValue (FIDC would disburse more than face value — critical loss).
        if (termInMonths <= 0)
            throw new InvalidPricingException(
                $"Term must be at least 1 month for ChequePredatado. Received: {termInMonths}. " +
                "Ensure dueDate is in the future.");

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
