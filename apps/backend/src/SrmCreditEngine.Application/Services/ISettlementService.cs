using SrmCreditEngine.Application.DTOs.Requests;
using SrmCreditEngine.Application.DTOs.Responses;

namespace SrmCreditEngine.Application.Services;

public interface ISettlementService
{
    Task<SettlementResponse> CreateAndSettleAsync(
        CreateSettlementRequest request,
        CancellationToken cancellationToken = default);

    Task<SettlementResponse> GetByIdAsync(
        Guid settlementId,
        CancellationToken cancellationToken = default);

    Task<SettlementStatementResponse> GetStatementAsync(
        GetSettlementStatementRequest request,
        CancellationToken cancellationToken = default);
}
