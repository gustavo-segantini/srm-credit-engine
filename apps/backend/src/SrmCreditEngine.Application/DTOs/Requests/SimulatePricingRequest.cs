using SrmCreditEngine.Domain.Enums;

namespace SrmCreditEngine.Application.DTOs.Requests;

public sealed record SimulatePricingRequest(
    decimal FaceValue,
    CurrencyCode FaceCurrency,
    CurrencyCode PaymentCurrency,
    ReceivableType ReceivableType,
    DateTime DueDate
);
