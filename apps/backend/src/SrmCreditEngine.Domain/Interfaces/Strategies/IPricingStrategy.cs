using SrmCreditEngine.Domain.Entities;
using SrmCreditEngine.Domain.Enums;
using SrmCreditEngine.Domain.ValueObjects;

namespace SrmCreditEngine.Domain.Interfaces.Strategies;

/// <summary>
/// Strategy Pattern interface for pricing receivables.
/// Each implementation encapsulates the risk spread rule for a specific product type.
/// </summary>
public interface IPricingStrategy
{
    ReceivableType SupportedType { get; }

    /// <summary>
    /// Calculates the present value of a receivable.
    /// Formula: PresentValue = FaceValue / (1 + BaseRate + Spread)^TermInMonths
    /// </summary>
    /// <param name="faceValue">Face value of the receivable.</param>
    /// <param name="termInMonths">Time to maturity in months.</param>
    /// <param name="baseRate">Monthly base rate (e.g., 0.01 = 1% a.m.).</param>
    /// <returns>Pricing result with full calculation breakdown.</returns>
    PricingResult Calculate(Money faceValue, int termInMonths, decimal baseRate);
}
