using SrmCreditEngine.Domain.Enums;
using SrmCreditEngine.Domain.Exceptions;
using SrmCreditEngine.Domain.ValueObjects;

namespace SrmCreditEngine.Domain.Entities;

/// <summary>
/// Represents the settlement (liquidação) of a receivable.
/// Uses optimistic locking (RowVersion) to prevent concurrent double-settlement.
/// </summary>
public sealed class Settlement : Entity
{
    public Guid ReceivableId { get; private set; }

    // Pricing inputs
    public decimal FaceValue { get; private set; }
    public CurrencyCode FaceCurrency { get; private set; }
    public decimal BaseRate { get; private set; }
    public decimal AppliedSpread { get; private set; }
    public int TermInMonths { get; private set; }

    // Pricing outputs
    public decimal PresentValue { get; private set; }
    public decimal Discount { get; private set; }
    public CurrencyCode PaymentCurrency { get; private set; }
    public decimal NetDisbursement { get; private set; }
    public decimal ExchangeRateApplied { get; private set; }

    public SettlementStatus Status { get; private set; }
    public DateTime? SettledAt { get; private set; }
    public string? FailureReason { get; private set; }

    /// <summary>Optimistic concurrency token — prevents race condition on settlement.</summary>
    public uint RowVersion { get; private set; }

    // Navigation
    public Receivable Receivable { get; private set; } = null!;

    private Settlement() { }  // EF Core

    public static Settlement CreatePending(
        Guid receivableId,
        PricingResult pricingResult)
    {
        return new Settlement
        {
            Id = Guid.NewGuid(),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            ReceivableId = receivableId,
            FaceValue = pricingResult.FaceValue.Amount,
            FaceCurrency = pricingResult.FaceValue.Currency,
            BaseRate = pricingResult.BaseRate,
            AppliedSpread = pricingResult.AppliedSpread,
            TermInMonths = pricingResult.TermInMonths,
            PresentValue = pricingResult.PresentValue.ToSettlementAmount(),
            Discount = pricingResult.Discount.ToSettlementAmount(),
            PaymentCurrency = pricingResult.NetDisbursement.Currency,
            NetDisbursement = pricingResult.NetDisbursement.ToSettlementAmount(),
            ExchangeRateApplied = pricingResult.ExchangeRateApplied,
            Status = SettlementStatus.Pending
        };
    }

    public void MarkAsSettled()
    {
        if (Status != SettlementStatus.Pending)
            throw new BusinessRuleViolationException(
                "SETTLEMENT_INVALID_STATE",
                $"Cannot settle a transaction in status '{Status}'.");

        Status = SettlementStatus.Settled;
        SettledAt = DateTime.UtcNow;
        TouchUpdatedAt();
    }

    public void MarkAsFailed(string reason)
    {
        if (Status == SettlementStatus.Settled)
            throw new BusinessRuleViolationException(
                "SETTLEMENT_ALREADY_SETTLED",
                "Cannot fail a transaction that is already settled.");

        Status = SettlementStatus.Failed;
        FailureReason = reason;
        TouchUpdatedAt();
    }

    public void Cancel()
    {
        if (Status == SettlementStatus.Settled)
            throw new BusinessRuleViolationException(
                "SETTLEMENT_ALREADY_SETTLED",
                "Cannot cancel a settled transaction.");

        Status = SettlementStatus.Cancelled;
        TouchUpdatedAt();
    }
}
