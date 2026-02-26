using SrmCreditEngine.Application.DTOs.Requests;
using SrmCreditEngine.Application.DTOs.Responses;

namespace SrmCreditEngine.Application.Services;

public interface IPricingService
{
    /// <summary>Simulates pricing without persisting — used for real-time UI preview.</summary>
    Task<PricingSimulationResponse> SimulateAsync(SimulatePricingRequest request, CancellationToken cancellationToken = default);
}
