using SrmCreditEngine.Application.DTOs.Requests;
using SrmCreditEngine.Application.DTOs.Responses;
using SrmCreditEngine.Application.Strategies;
using SrmCreditEngine.Domain.Enums;
using SrmCreditEngine.Domain.Exceptions;
using SrmCreditEngine.Domain.Interfaces.Repositories;
using SrmCreditEngine.Domain.ValueObjects;

namespace SrmCreditEngine.Application.Services;

public sealed class PricingService : IPricingService
{
    // Base monthly rate (CDI-like proxy). In production, fetch from a rate provider.
    private const decimal DefaultBaseRateMonthly = 0.0089m; // ~0.89% a.m. ≈ 11% a.a.

    private readonly PricingStrategyFactory _strategyFactory;
    private readonly IExchangeRateRepository _exchangeRateRepository;

    public PricingService(
        PricingStrategyFactory strategyFactory,
        IExchangeRateRepository exchangeRateRepository)
    {
        _strategyFactory = strategyFactory;
        _exchangeRateRepository = exchangeRateRepository;
    }

    public async Task<PricingSimulationResponse> SimulateAsync(
        SimulatePricingRequest request,
        CancellationToken cancellationToken = default)
    {
        var strategy = _strategyFactory.Resolve(request.ReceivableType);
        var faceValue = new Money(request.FaceValue, request.FaceCurrency);

        var dueDate = request.DueDate.ToUniversalTime();
        var today = DateTime.UtcNow;
        var termInMonths = Math.Max(1, (int)Math.Ceiling((dueDate - today).TotalDays / 30.0));

        var result = strategy.Calculate(faceValue, termInMonths, DefaultBaseRateMonthly);

        // Cross-currency conversion
        var netDisbursement = result.NetDisbursement;
        var exchangeRateApplied = 1m;

        if (request.FaceCurrency != request.PaymentCurrency)
        {
            var rate = await _exchangeRateRepository.GetLatestAsync(
                request.FaceCurrency,
                request.PaymentCurrency,
                cancellationToken: cancellationToken);

            if (rate == null)
                throw new ExchangeRateNotFoundException(
                    request.FaceCurrency.ToString(),
                    request.PaymentCurrency.ToString());

            exchangeRateApplied = rate.Rate;
            netDisbursement = result.PresentValue.ConvertTo(request.PaymentCurrency, rate.Rate);
        }

        var discountRate = result.FaceValue.Amount > 0
            ? result.Discount.Amount / result.FaceValue.Amount
            : 0m;

        return new PricingSimulationResponse(
            FaceValue: result.FaceValue.ToSettlementAmount(),
            FaceCurrency: result.FaceValue.Currency.ToString(),
            PresentValue: result.PresentValue.ToSettlementAmount(),
            Discount: result.Discount.ToSettlementAmount(),
            DiscountRatePercent: Math.Round(discountRate * 100, 4),
            AppliedSpreadPercent: result.AppliedSpread * 100,
            BaseRatePercent: result.BaseRate * 100,
            TermInMonths: termInMonths,
            NetDisbursement: netDisbursement.ToSettlementAmount(),
            PaymentCurrency: request.PaymentCurrency.ToString(),
            ExchangeRateApplied: exchangeRateApplied,
            IsCrossCurrency: request.FaceCurrency != request.PaymentCurrency,
            SimulatedAt: DateTime.UtcNow
        );
    }
}
