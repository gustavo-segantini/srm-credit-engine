using SrmCreditEngine.Domain.Enums;

namespace SrmCreditEngine.Application.DTOs.Requests;

public sealed record CreateSettlementRequest(
    Guid CedentId,
    string DocumentNumber,
    ReceivableType ReceivableType,
    decimal FaceValue,
    CurrencyCode FaceCurrency,
    CurrencyCode PaymentCurrency,
    DateTime DueDate
);
