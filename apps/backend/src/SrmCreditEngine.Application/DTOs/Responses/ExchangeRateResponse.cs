namespace SrmCreditEngine.Application.DTOs.Responses;

public sealed record ExchangeRateResponse(
    Guid Id,
    string FromCurrency,
    string ToCurrency,
    decimal Rate,
    DateTime EffectiveDate,
    DateTime? ExpiresAt,
    string Source,
    DateTime UpdatedAt
);
