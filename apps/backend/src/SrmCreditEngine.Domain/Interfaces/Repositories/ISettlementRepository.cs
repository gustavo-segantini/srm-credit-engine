using SrmCreditEngine.Domain.Entities;
using SrmCreditEngine.Domain.Enums;

namespace SrmCreditEngine.Domain.Interfaces.Repositories;

public interface ISettlementRepository : IRepository<Settlement>
{
    Task<Settlement?> GetByReceivableIdAsync(Guid receivableId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns an optimistically-locked settlement for update operations.
    /// </summary>
    Task<Settlement?> GetForUpdateAsync(Guid settlementId, CancellationToken cancellationToken = default);
}
