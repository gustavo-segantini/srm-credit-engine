using SrmCreditEngine.Application.DTOs.Requests;
using SrmCreditEngine.Application.DTOs.Responses;
using SrmCreditEngine.Application.Interfaces;
using SrmCreditEngine.Domain.Entities;
using SrmCreditEngine.Domain.Enums;
using SrmCreditEngine.Domain.Exceptions;
using SrmCreditEngine.Domain.Interfaces.Repositories;

namespace SrmCreditEngine.Application.Services;

public sealed class CurrencyService : ICurrencyService
{
    private readonly IExchangeRateRepository _exchangeRateRepo;
    private readonly ICurrencyRepository _currencyRepo;
    private readonly IUnitOfWork _unitOfWork;

    public CurrencyService(
        IExchangeRateRepository exchangeRateRepo,
        ICurrencyRepository currencyRepo,
        IUnitOfWork unitOfWork)
    {
        _exchangeRateRepo = exchangeRateRepo;
        _currencyRepo = currencyRepo;
        _unitOfWork = unitOfWork;
    }

    public async Task<ExchangeRateResponse> GetLatestExchangeRateAsync(
        CurrencyCode from,
        CurrencyCode to,
        CancellationToken cancellationToken = default)
    {
        var rate = await _exchangeRateRepo.GetLatestAsync(from, to, cancellationToken: cancellationToken);

        if (rate == null)
            throw new ExchangeRateNotFoundException(from.ToString(), to.ToString());

        return MapToResponse(rate);
    }

    public async Task<ExchangeRateResponse> UpsertExchangeRateAsync(
        UpdateExchangeRateRequest request,
        CancellationToken cancellationToken = default)
    {
        var fromCurrency = await _currencyRepo.GetByCodeAsync(request.FromCurrency, cancellationToken)
            ?? throw new BusinessRuleViolationException("CURRENCY_NOT_FOUND", $"Currency {request.FromCurrency} not found.");

        var toCurrency = await _currencyRepo.GetByCodeAsync(request.ToCurrency, cancellationToken)
            ?? throw new BusinessRuleViolationException("CURRENCY_NOT_FOUND", $"Currency {request.ToCurrency} not found.");

        ExchangeRate exchangeRate;
        var existing = await _exchangeRateRepo.GetLatestAsync(
            request.FromCurrency, request.ToCurrency, cancellationToken: cancellationToken);

        if (existing != null)
        {
            existing.Update(request.Rate, request.Source);
            _exchangeRateRepo.Update(existing);
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

            await _exchangeRateRepo.AddAsync(exchangeRate, cancellationToken);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return MapToResponse(exchangeRate);
    }

    public async Task<IReadOnlyList<ExchangeRateResponse>> GetRateHistoryAsync(
        CurrencyCode from,
        CurrencyCode to,
        DateTime fromDate,
        DateTime toDate,
        CancellationToken cancellationToken = default)
    {
        var rates = await _exchangeRateRepo.GetHistoryAsync(from, to, fromDate, toDate, cancellationToken);
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
