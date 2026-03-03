using SrmCreditEngine.Application.DTOs.Requests;
using SrmCreditEngine.Application.DTOs.Responses;
using SrmCreditEngine.Application.Interfaces;
using SrmCreditEngine.Domain.Entities;
using SrmCreditEngine.Domain.Enums;
using SrmCreditEngine.Domain.Exceptions;
using SrmCreditEngine.Domain.Interfaces.Repositories;

namespace SrmCreditEngine.Application.Services;

public sealed class CurrencyService(
    IExchangeRateRepository exchangeRateRepo,
    ICurrencyRepository currencyRepo,
    IUnitOfWork unitOfWork) : ICurrencyService
{
    public async Task<ExchangeRateResponse> GetLatestExchangeRateAsync(
        CurrencyCode from,
        CurrencyCode to,
        CancellationToken cancellationToken = default)
    {
        var rate = await exchangeRateRepo.GetLatestAsync(from, to, cancellationToken: cancellationToken);

        if (rate == null)
        {
            throw new ExchangeRateNotFoundException(from.ToString(), to.ToString());
        }

        return MapToResponse(rate);
    }

    public async Task<ExchangeRateResponse> UpsertExchangeRateAsync(
        UpdateExchangeRateRequest request,
        CancellationToken cancellationToken = default)
    {
        var fromCurrency = await currencyRepo.GetByCodeAsync(request.FromCurrency, cancellationToken)
            ?? throw new BusinessRuleViolationException("CURRENCY_NOT_FOUND", $"Currency {request.FromCurrency} not found.");

        var toCurrency = await currencyRepo.GetByCodeAsync(request.ToCurrency, cancellationToken)
            ?? throw new BusinessRuleViolationException("CURRENCY_NOT_FOUND", $"Currency {request.ToCurrency} not found.");

        ExchangeRate exchangeRate;
        var existing = await exchangeRateRepo.GetLatestAsync(
            request.FromCurrency, request.ToCurrency, cancellationToken: cancellationToken);

        if (existing != null)
        {
            existing.Update(request.Rate, request.Source);
            exchangeRateRepo.Update(existing);
            exchangeRate = existing;
        }
        else
        {
            exchangeRate = new ExchangeRate(
                fromCurrency.Id,
                toCurrency.Id,
                request.Rate,
                DateTime.UtcNow,
                request.Source);

            await exchangeRateRepo.AddAsync(exchangeRate, cancellationToken);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return MapToResponse(exchangeRate);
    }

    public async Task<IReadOnlyList<ExchangeRateResponse>> GetRateHistoryAsync(
        CurrencyCode from,
        CurrencyCode to,
        DateTime fromDate,
        DateTime toDate,
        CancellationToken cancellationToken = default)
    {
        var rates = await exchangeRateRepo.GetHistoryAsync(from, to, fromDate, toDate, cancellationToken);
        return rates.Select(MapToResponse).ToList();
    }

    private static ExchangeRateResponse MapToResponse(ExchangeRate rate) =>
        new(
            Id: rate.Id,
            FromCurrency: rate.FromCurrency?.Code.ToString() ?? rate.FromCurrencyId.ToString(),
            ToCurrency: rate.ToCurrency?.Code.ToString() ?? rate.ToCurrencyId.ToString(),
            Rate: rate.Rate,
            EffectiveDate: rate.EffectiveDate,
            ExpiresAt: rate.ExpiresAt,
            Source: rate.Source,
            UpdatedAt: rate.UpdatedAt
        );
}
