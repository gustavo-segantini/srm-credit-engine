using SrmCreditEngine.Domain.Enums;

namespace SrmCreditEngine.Application.DTOs.Responses;

public sealed record SettlementResponse(
    Guid Id,
    Guid ReceivableId,
    string DocumentNumber,
    string CedentName,
    string CedentCnpj,
    string ReceivableType,
    decimal FaceValue,
    string FaceCurrency,
    decimal PresentValue,
    decimal Discount,
    decimal DiscountRatePercent,
    decimal AppliedSpreadPercent,
    decimal BaseRatePercent,
    int TermInMonths,
    decimal NetDisbursement,
    string PaymentCurrency,
    decimal ExchangeRateApplied,
    bool IsCrossCurrency,
    string Status,
    DateTime? SettledAt,
    string? FailureReason,
    DateTime CreatedAt
);
