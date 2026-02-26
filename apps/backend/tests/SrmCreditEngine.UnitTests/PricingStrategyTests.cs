using FluentAssertions;
using SrmCreditEngine.Application.Strategies;
using SrmCreditEngine.Domain.Enums;
using SrmCreditEngine.Domain.ValueObjects;

namespace SrmCreditEngine.UnitTests;

/// <summary>
/// Validates the core pricing formula:
/// PresentValue = FaceValue / (1 + BaseRate + Spread)^TermInMonths
/// </summary>
public class PricingStrategyTests
{
    private const decimal DefaultBaseRate = 0.0089m; // 0.89% a.m.

    // ── DuplicataMercantil ────────────────────────────────────────────────
    [Fact]
    public void DuplicataMercantil_SupportedType_IsDuplicataMercantil()
    {
        var strategy = new DuplicataMercantilPricingStrategy();
        strategy.SupportedType.Should().Be(ReceivableType.DuplicataMercantil);
    }

    [Theory]
    [InlineData(10_000.00, 3)]   // 3-month term
    [InlineData(50_000.00, 6)]   // 6-month term
    [InlineData(100_000.00, 1)]  // same-month
    public void DuplicataMercantil_PresentValue_ShouldBeLessThanFaceValue(
        decimal faceValue, int termInMonths)
    {
        var strategy = new DuplicataMercantilPricingStrategy();
        var face     = new Money(faceValue, CurrencyCode.BRL);
        var result   = strategy.Calculate(face, termInMonths, DefaultBaseRate);

        result.PresentValue.Amount.Should().BeLessThan(faceValue);
    }

    [Fact]
    public void DuplicataMercantil_Spread_Is_1Point5Percent()
    {
        var strategy = new DuplicataMercantilPricingStrategy();
        var face     = new Money(10_000m, CurrencyCode.BRL);
        var result   = strategy.Calculate(face, 3, DefaultBaseRate);

        result.AppliedSpread.Should().Be(0.015m);
    }

    [Fact]
    public void DuplicataMercantil_Discount_Equals_FaceValue_Minus_PresentValue()
    {
        var strategy = new DuplicataMercantilPricingStrategy();
        var face     = new Money(10_000m, CurrencyCode.BRL);
        var result   = strategy.Calculate(face, 3, DefaultBaseRate);

        result.Discount.Amount.Should().BeApproximately(
            result.FaceValue.Amount - result.PresentValue.Amount, 0.0001m);
    }

    [Fact]
    public void DuplicataMercantil_Formula_ManualVerification()
    {
        // PV = 10_000 / (1 + 0.0089 + 0.015)^3
        var strategy     = new DuplicataMercantilPricingStrategy();
        var face         = new Money(10_000m, CurrencyCode.BRL);
        var result       = strategy.Calculate(face, 3, DefaultBaseRate);

        var expectedRate = 1m + DefaultBaseRate + 0.015m;
        var expectedPv   = 10_000m / (decimal)Math.Pow((double)expectedRate, 3);

        result.PresentValue.Amount.Should().BeApproximately(expectedPv, 0.01m);
    }

    // ── ChequePredatado ───────────────────────────────────────────────────
    [Fact]
    public void ChequePredatado_SupportedType_IsChequePredatado()
    {
        var strategy = new ChequePredatadoPricingStrategy();
        strategy.SupportedType.Should().Be(ReceivableType.ChequePredatado);
    }

    [Fact]
    public void ChequePredatado_Spread_Is_2Point5Percent()
    {
        var strategy = new ChequePredatadoPricingStrategy();
        var face     = new Money(10_000m, CurrencyCode.BRL);
        var result   = strategy.Calculate(face, 3, DefaultBaseRate);

        result.AppliedSpread.Should().Be(0.025m);
    }

    [Fact]
    public void ChequePredatado_PresentValue_LowerThan_DuplicataMercantil_ForSameInput()
    {
        // ChequePredatado has higher spread → lower present value → bigger discount
        var duplicata = new DuplicataMercantilPricingStrategy();
        var cheque    = new ChequePredatadoPricingStrategy();
        var face      = new Money(10_000m, CurrencyCode.BRL);

        var rDup  = duplicata.Calculate(face, 3, DefaultBaseRate);
        var rCheq = cheque.Calculate(face, 3, DefaultBaseRate);

        rCheq.PresentValue.Amount.Should().BeLessThan(rDup.PresentValue.Amount);
    }

    [Fact]
    public void ChequePredatado_Discount_IsGreaterThan_DuplicataMercantil()
    {
        var duplicata = new DuplicataMercantilPricingStrategy();
        var cheque    = new ChequePredatadoPricingStrategy();
        var face      = new Money(10_000m, CurrencyCode.BRL);

        var rDup  = duplicata.Calculate(face, 3, DefaultBaseRate);
        var rCheq = cheque.Calculate(face, 3, DefaultBaseRate);

        rCheq.Discount.Amount.Should().BeGreaterThan(rDup.Discount.Amount);
    }

    // ── Edge cases ────────────────────────────────────────────────────────
    [Fact]
    public void Strategy_OneMonthTerm_DiscountIsPositive()
    {
        var strategy = new DuplicataMercantilPricingStrategy();
        var face     = new Money(1_000m, CurrencyCode.BRL);
        var result   = strategy.Calculate(face, 1, DefaultBaseRate);

        result.Discount.Amount.Should().BePositive();
        result.PresentValue.Amount.Should().BePositive();
        result.PresentValue.Amount.Should().BeLessThan(1_000m);
    }

    [Fact]
    public void Strategy_ZeroBaseRate_SpreadStillApplies()
    {
        // When base rate is zero, only spread drives the discount
        var strategy = new DuplicataMercantilPricingStrategy();
        var face     = new Money(5_000m, CurrencyCode.BRL);
        var result   = strategy.Calculate(face, 2, baseRate: 0m);

        result.PresentValue.Amount.Should().BeLessThan(5_000m);
        result.Discount.Amount.Should().BePositive();
    }
}
