using SrmCreditEngine.Domain.Enums;

namespace SrmCreditEngine.Application.DTOs.Requests;

public sealed record GetSettlementStatementRequest(
    DateTime? From,
    DateTime? To,
    Guid? CedentId,
    CurrencyCode? PaymentCurrency,
    int Page = 1,
    int PageSize = 50
);
