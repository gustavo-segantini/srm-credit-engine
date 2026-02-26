using SrmCreditEngine.Domain.Enums;

namespace SrmCreditEngine.Application.DTOs.Responses;

public sealed record PricingSimulationResponse(
    decimal FaceValue,
    string FaceCurrency,
    decimal PresentValue,
    decimal Discount,
    decimal DiscountRatePercent,
    decimal AppliedSpreadPercent,
    decimal BaseRatePercent,
    int TermInMonths,
    decimal NetDisbursement,
    string PaymentCurrency,
    decimal ExchangeRateApplied,
    bool IsCrossCurrency,
    DateTime SimulatedAt
);
