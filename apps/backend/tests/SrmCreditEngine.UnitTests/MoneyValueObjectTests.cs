using FluentAssertions;
using SrmCreditEngine.Domain.Enums;
using SrmCreditEngine.Domain.ValueObjects;

namespace SrmCreditEngine.UnitTests;

public class MoneyValueObjectTests
{
    // ── Construction ──────────────────────────────────────────────────────
    [Fact]
    public void Constructor_SetsAmountAndCurrency()
    {
        var m = new Money(100m, CurrencyCode.BRL);
        m.Amount.Should().Be(100m);
        m.Currency.Should().Be(CurrencyCode.BRL);
    }

    [Fact]
    public void Constructor_NegativeAmount_ThrowsArgumentException()
    {
        var act = () => new Money(-1m, CurrencyCode.BRL);
        act.Should().Throw<Exception>();
    }

    // ── Arithmetic ────────────────────────────────────────────────────────
    [Fact]
    public void Add_SameCurrency_ReturnsSumSameCurrency()
    {
        var a = new Money(100m, CurrencyCode.BRL);
        var b = new Money(50m, CurrencyCode.BRL);
        var result = a.Add(b);
        result.Amount.Should().Be(150m);
        result.Currency.Should().Be(CurrencyCode.BRL);
    }

    [Fact]
    public void Add_DifferentCurrency_ThrowsException()
    {
        var a = new Money(100m, CurrencyCode.BRL);
        var b = new Money(50m,  CurrencyCode.USD);
        var act = () => a.Add(b);
        act.Should().Throw<Exception>();
    }

    [Fact]
    public void Subtract_ResultIsCorrect()
    {
        var a = new Money(200m, CurrencyCode.USD);
        var b = new Money(50m,  CurrencyCode.USD);
        var result = a.Subtract(b);
        result.Amount.Should().Be(150m);
    }

    [Fact]
    public void Multiply_ScalesByFactor()
    {
        var m = new Money(100m, CurrencyCode.BRL);
        var result = m.Multiply(2.5m);
        result.Amount.Should().Be(250m);
        result.Currency.Should().Be(CurrencyCode.BRL);
    }

    [Fact]
    public void Divide_ScalesCorrectly()
    {
        var m = new Money(300m, CurrencyCode.BRL);
        var result = m.Divide(3m);
        result.Amount.Should().Be(100m);
    }

    [Fact]
    public void Divide_ByZero_ThrowsException()
    {
        var m   = new Money(100m, CurrencyCode.BRL);
        var act = () => m.Divide(0m);
        act.Should().Throw<Exception>();
    }

    // ── Cross-currency conversion ─────────────────────────────────────────
    [Fact]
    public void ConvertTo_ChangescurrencyAndMultipliesByRate()
    {
        var usd    = new Money(100m, CurrencyCode.USD);
        var brl    = usd.ConvertTo(CurrencyCode.BRL, exchangeRate: 5.75m);
        brl.Amount.Should().BeApproximately(575m, 0.001m);
        brl.Currency.Should().Be(CurrencyCode.BRL);
    }

    [Fact]
    public void ConvertTo_SameCurrency_RateOne_ReturnsEquivalent()
    {
        var m      = new Money(1_000m, CurrencyCode.BRL);
        var result = m.ConvertTo(CurrencyCode.BRL, 1m);
        result.Amount.Should().Be(1_000m);
    }

    // ── Settlement rounding ───────────────────────────────────────────────
    [Fact]
    public void ToSettlementAmount_RoundsToTwoDecimalPlaces()
    {
        var m      = new Money(100.128m, CurrencyCode.BRL);
        var result = m.ToSettlementAmount();
        // Verify no more than 2 decimal places
        var rounded = Math.Round(result, 2);
        rounded.Should().Be(result);
    }

    // ── Equality (value object semantics) ─────────────────────────────────
    [Fact]
    public void TwoMoney_WithSameAmountAndCurrency_AreEqual()
    {
        var a = new Money(100m, CurrencyCode.BRL);
        var b = new Money(100m, CurrencyCode.BRL);
        a.Should().Be(b);
    }

    [Fact]
    public void TwoMoney_WithDifferentCurrency_AreNotEqual()
    {
        var a = new Money(100m, CurrencyCode.BRL);
        var b = new Money(100m, CurrencyCode.USD);
        a.Should().NotBe(b);
    }
}
