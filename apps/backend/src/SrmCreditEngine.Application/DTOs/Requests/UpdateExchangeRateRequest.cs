using SrmCreditEngine.Domain.Enums;

namespace SrmCreditEngine.Application.DTOs.Requests;

public sealed record UpdateExchangeRateRequest(
    CurrencyCode FromCurrency,
    CurrencyCode ToCurrency,
    decimal Rate,
    string Source
);
