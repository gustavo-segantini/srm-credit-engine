using SrmCreditEngine.Domain.Entities;

namespace SrmCreditEngine.Domain.Interfaces.Repositories;

public interface IReceivableRepository : IRepository<Receivable>
{
    Task<Receivable?> GetByDocumentNumberAsync(string documentNumber, Guid cedentId, CancellationToken cancellationToken = default);
    Task<bool> HasSettlementAsync(Guid receivableId, CancellationToken cancellationToken = default);
}
