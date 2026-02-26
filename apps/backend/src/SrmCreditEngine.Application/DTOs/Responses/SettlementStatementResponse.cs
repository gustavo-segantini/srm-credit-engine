namespace SrmCreditEngine.Application.DTOs.Responses;

public sealed record SettlementStatementItemResponse(
    Guid SettlementId,
    Guid ReceivableId,
    string DocumentNumber,
    string CedentName,
    string CedentCnpj,
    string ReceivableType,
    decimal FaceValue,
    string FaceCurrency,
    decimal NetDisbursement,
    string PaymentCurrency,
    decimal Discount,
    decimal DiscountRatePercent,
    string Status,
    DateTime? SettledAt,
    DateTime CreatedAt
);

public sealed record SettlementStatementResponse(
    IReadOnlyList<SettlementStatementItemResponse> Items,
    int TotalCount,
    int Page,
    int PageSize,
    int TotalPages,
    decimal TotalFaceValue,
    decimal TotalNetDisbursement,
    decimal TotalDiscount
);
