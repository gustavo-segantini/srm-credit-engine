using SrmCreditEngine.Application.DTOs.Requests;
using SrmCreditEngine.Application.DTOs.Responses;

namespace SrmCreditEngine.Application.Interfaces;

/// <summary>
/// Analytics query interface — bypasses domain layer for optimized raw SQL reporting.
/// Follows the case's requirement: reports use 2-layer architecture (API → Infrastructure direct).
/// Implemented in Infrastructure using Dapper for performance.
/// </summary>
public interface ISettlementStatementQuery
{
    Task<SettlementStatementResponse> GetStatementAsync(
        GetSettlementStatementRequest request,
        CancellationToken cancellationToken = default);
}
