using SrmCreditEngine.Application.DTOs.Requests;
using SrmCreditEngine.Application.DTOs.Responses;
using SrmCreditEngine.Domain.Enums;

namespace SrmCreditEngine.Application.Services;

public interface ICurrencyService
{
    Task<ExchangeRateResponse> GetLatestExchangeRateAsync(
        CurrencyCode from,
        CurrencyCode to,
        CancellationToken cancellationToken = default);

    Task<ExchangeRateResponse> UpsertExchangeRateAsync(
        UpdateExchangeRateRequest request,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ExchangeRateResponse>> GetRateHistoryAsync(
        CurrencyCode from,
        CurrencyCode to,
        DateTime fromDate,
        DateTime toDate,
        CancellationToken cancellationToken = default);
}
