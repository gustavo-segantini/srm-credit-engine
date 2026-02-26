using SrmCreditEngine.Application.DTOs.Requests;
using SrmCreditEngine.Application.DTOs.Responses;
using SrmCreditEngine.Application.Interfaces;
using SrmCreditEngine.Application.Strategies;
using SrmCreditEngine.Domain.Entities;
using SrmCreditEngine.Domain.Exceptions;
using SrmCreditEngine.Domain.Interfaces.Repositories;
using SrmCreditEngine.Domain.ValueObjects;

namespace SrmCreditEngine.Application.Services;

public sealed class SettlementService : ISettlementService
{
    private const decimal DefaultBaseRateMonthly = 0.0089m; // ~0.89% a.m.

    private readonly ISettlementRepository _settlementRepo;
    private readonly IReceivableRepository _receivableRepo;
    private readonly ICedentRepository _cedentRepo;
    private readonly IExchangeRateRepository _exchangeRateRepo;
    private readonly PricingStrategyFactory _strategyFactory;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ISettlementStatementQuery _statementQuery;

    public SettlementService(
        ISettlementRepository settlementRepo,
        IReceivableRepository receivableRepo,
        ICedentRepository cedentRepo,
        IExchangeRateRepository exchangeRateRepo,
        PricingStrategyFactory strategyFactory,
        IUnitOfWork unitOfWork,
        ISettlementStatementQuery statementQuery)
    {
        _settlementRepo = settlementRepo;
        _receivableRepo = receivableRepo;
        _cedentRepo = cedentRepo;
        _exchangeRateRepo = exchangeRateRepo;
        _strategyFactory = strategyFactory;
        _unitOfWork = unitOfWork;
        _statementQuery = statementQuery;
    }

    public async Task<SettlementResponse> CreateAndSettleAsync(
        CreateSettlementRequest request,
        CancellationToken cancellationToken = default)
    {
        // Validate cedent exists
        var cedent = await _cedentRepo.GetByIdAsync(request.CedentId, cancellationToken)
            ?? throw new BusinessRuleViolationException("CEDENT_NOT_FOUND", $"Cedent {request.CedentId} not found.");

        // Idempotency — check if document already exists for this cedent
        var existingReceivable = await _receivableRepo.GetByDocumentNumberAsync(
            request.DocumentNumber, request.CedentId, cancellationToken);

        if (existingReceivable != null)
        {
            var existingSettlement = await _settlementRepo.GetByReceivableIdAsync(
                existingReceivable.Id, cancellationToken);

            if (existingSettlement != null)
                throw new BusinessRuleViolationException(
                    "DUPLICATE_SETTLEMENT",
                    $"Document '{request.DocumentNumber}' has already been settled.");
        }

        // Create receivable
        var receivable = new Receivable(
            request.CedentId,
            request.DocumentNumber,
            request.ReceivableType,
            request.FaceValue,
            request.FaceCurrency,
            request.DueDate);

        var termInMonths = receivable.GetTermInMonths(DateTime.UtcNow);

        // Price the receivable
        var strategy = _strategyFactory.Resolve(request.ReceivableType);
        var faceValue = new Money(request.FaceValue, request.FaceCurrency);
        var pricingResult = strategy.Calculate(faceValue, termInMonths, DefaultBaseRateMonthly);

        // Cross-currency conversion
        var finalPricingResult = pricingResult;
        if (request.FaceCurrency != request.PaymentCurrency)
        {
            var rate = await _exchangeRateRepo.GetLatestAsync(
                request.FaceCurrency, request.PaymentCurrency, cancellationToken: cancellationToken)
                ?? throw new ExchangeRateNotFoundException(
                    request.FaceCurrency.ToString(), request.PaymentCurrency.ToString());

            var convertedDisbursement = pricingResult.PresentValue.ConvertTo(request.PaymentCurrency, rate.Rate);

            finalPricingResult = new PricingResult(
                faceValue: pricingResult.FaceValue,
                presentValue: pricingResult.PresentValue,
                appliedSpread: pricingResult.AppliedSpread,
                baseRate: pricingResult.BaseRate,
                termInMonths: pricingResult.TermInMonths,
                netDisbursement: convertedDisbursement,
                exchangeRateApplied: rate.Rate);
        }

        Settlement settlement = default!;

        // ACID transaction: persist receivable + settlement atomically
        await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            await _receivableRepo.AddAsync(receivable, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            settlement = Settlement.CreatePending(receivable.Id, finalPricingResult);
            await _settlementRepo.AddAsync(settlement, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // Settle immediately (fund acquires the receivable)
            settlement.MarkAsSettled();
            _settlementRepo.Update(settlement);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }, cancellationToken);

        return MapToResponse(settlement, receivable, cedent);
    }

    public async Task<SettlementResponse> GetByIdAsync(
        Guid settlementId,
        CancellationToken cancellationToken = default)
    {
        var settlement = await _settlementRepo.GetByIdAsync(settlementId, cancellationToken)
            ?? throw new BusinessRuleViolationException("SETTLEMENT_NOT_FOUND", $"Settlement {settlementId} not found.");

        var receivable = await _receivableRepo.GetByIdAsync(settlement.ReceivableId, cancellationToken)
            ?? throw new BusinessRuleViolationException("RECEIVABLE_NOT_FOUND", "Associated receivable not found.");

        var cedent = await _cedentRepo.GetByIdAsync(receivable.CedentId, cancellationToken)
            ?? throw new BusinessRuleViolationException("CEDENT_NOT_FOUND", "Associated cedent not found.");

        return MapToResponse(settlement, receivable, cedent);
    }

    public Task<SettlementStatementResponse> GetStatementAsync(
        GetSettlementStatementRequest request,
        CancellationToken cancellationToken = default)
        => _statementQuery.GetStatementAsync(request, cancellationToken);

    private static SettlementResponse MapToResponse(Settlement s, Receivable r, Domain.Entities.Cedent c)
    {
        var discountRate = s.FaceValue > 0 ? s.Discount / s.FaceValue : 0m;

        return new SettlementResponse(
            Id: s.Id,
            ReceivableId: r.Id,
            DocumentNumber: r.DocumentNumber,
            CedentName: c.Name,
            CedentCnpj: c.Cnpj,
            ReceivableType: r.Type.ToString(),
            FaceValue: s.FaceValue,
            FaceCurrency: s.FaceCurrency.ToString(),
            PresentValue: s.PresentValue,
            Discount: s.Discount,
            DiscountRatePercent: Math.Round(discountRate * 100, 4),
            AppliedSpreadPercent: s.AppliedSpread * 100,
            BaseRatePercent: s.BaseRate * 100,
            TermInMonths: s.TermInMonths,
            NetDisbursement: s.NetDisbursement,
            PaymentCurrency: s.PaymentCurrency.ToString(),
            ExchangeRateApplied: s.ExchangeRateApplied,
            IsCrossCurrency: s.FaceCurrency != s.PaymentCurrency,
            Status: s.Status.ToString(),
            SettledAt: s.SettledAt,
            FailureReason: s.FailureReason,
            CreatedAt: s.CreatedAt
        );
    }
}
