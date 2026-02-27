using SrmCreditEngine.Application.DTOs.Requests;
using SrmCreditEngine.Application.DTOs.Responses;

namespace SrmCreditEngine.Application.Services;

public interface ICedentService
{
    Task<CedentResponse> CreateAsync(CreateCedentRequest request, CancellationToken cancellationToken = default);
    Task<CedentResponse> UpdateAsync(Guid id, UpdateCedentRequest request, CancellationToken cancellationToken = default);
    Task DeactivateAsync(Guid id, CancellationToken cancellationToken = default);
    Task<CedentResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<CedentResponse>> GetAllActiveAsync(CancellationToken cancellationToken = default);
}
