using FluentAssertions;
using SrmCreditEngine.Domain.Entities;
using SrmCreditEngine.Domain.Enums;
using SrmCreditEngine.Domain.Exceptions;
using SrmCreditEngine.Domain.ValueObjects;

namespace SrmCreditEngine.UnitTests;

/// <summary>
/// Tests the Settlement state machine:
/// Pending → Settled | Failed | Cancelled
/// </summary>
public class SettlementEntityTests
{
    // ── Factory helper ────────────────────────────────────────────────────
    private static Settlement MakePending(decimal faceValue = 10_000m)
    {
        var face           = new Money(faceValue, CurrencyCode.BRL);
        var presentValue   = new Money(faceValue * 0.95m, CurrencyCode.BRL);
        var pricingResult  = new PricingResult(
            faceValue:         face,
            presentValue:      presentValue,
            appliedSpread:     0.015m,
            baseRate:          0.0089m,
            termInMonths:      3,
            netDisbursement:   presentValue,
            exchangeRateApplied: 1.0m);

        return Settlement.CreatePending(receivableId: Guid.NewGuid(), pricingResult);
    }

    // ── CreatePending ─────────────────────────────────────────────────────
    [Fact]
    public void CreatePending_Status_IsPending()
    {
        var s = MakePending();
        s.Status.Should().Be(SettlementStatus.Pending);
    }

    [Fact]
    public void CreatePending_SettledAt_IsNull()
    {
        var s = MakePending();
        s.SettledAt.Should().BeNull();
    }

    [Fact]
    public void CreatePending_FaceValue_IsPreserved()
    {
        var s = MakePending(faceValue: 25_000m);
        s.FaceValue.Should().Be(25_000m);
    }

    // ── MarkAsSettled ─────────────────────────────────────────────────────
    [Fact]
    public void MarkAsSettled_TransitionsTo_Settled()
    {
        var s = MakePending();
        s.MarkAsSettled();
        s.Status.Should().Be(SettlementStatus.Settled);
    }

    [Fact]
    public void MarkAsSettled_SetsSettledAt()
    {
        var before = DateTime.UtcNow;
        var s      = MakePending();
        s.MarkAsSettled();
        s.SettledAt.Should().NotBeNull();
        s.SettledAt!.Value.Should().BeOnOrAfter(before);
    }

    [Fact]
    public void MarkAsSettled_OnAlreadySettled_ThrowsBusinessRuleViolation()
    {
        var s = MakePending();
        s.MarkAsSettled();

        // Cannot settle twice
        var act = () => s.MarkAsSettled();
        act.Should().Throw<BusinessRuleViolationException>()
            .Which.Code.Should().Be("SETTLEMENT_INVALID_STATE");
    }

    // ── MarkAsFailed ──────────────────────────────────────────────────────
    [Fact]
    public void MarkAsFailed_TransitionsTo_Failed()
    {
        var s = MakePending();
        s.MarkAsFailed("Insufficient funds");
        s.Status.Should().Be(SettlementStatus.Failed);
    }

    [Fact]
    public void MarkAsFailed_SetsFailureReason()
    {
        var s = MakePending();
        s.MarkAsFailed("Bank rejected");
        s.FailureReason.Should().Be("Bank rejected");
    }

    [Fact]
    public void MarkAsFailed_OnSettled_ThrowsBusinessRuleViolation()
    {
        var s = MakePending();
        s.MarkAsSettled();

        var act = () => s.MarkAsFailed("Test");
        act.Should().Throw<BusinessRuleViolationException>()
            .Which.Code.Should().Be("SETTLEMENT_ALREADY_SETTLED");
    }

    // ── Cancel ────────────────────────────────────────────────────────────
    [Fact]
    public void Cancel_TransitionsTo_Cancelled()
    {
        var s = MakePending();
        s.Cancel();
        s.Status.Should().Be(SettlementStatus.Cancelled);
    }

    [Fact]
    public void Cancel_OnSettled_ThrowsBusinessRuleViolation()
    {
        var s = MakePending();
        s.MarkAsSettled();

        var act = () => s.Cancel();
        act.Should().Throw<BusinessRuleViolationException>()
            .Which.Code.Should().Be("SETTLEMENT_ALREADY_SETTLED");
    }

    [Fact]
    public void Cancel_OnFailed_Succeeds()
    {
        // Business allows cancellation of failed settlements for auditing
        var s = MakePending();
        s.MarkAsFailed("Testing");
        s.Cancel(); // should not throw
        s.Status.Should().Be(SettlementStatus.Cancelled);
    }
}
