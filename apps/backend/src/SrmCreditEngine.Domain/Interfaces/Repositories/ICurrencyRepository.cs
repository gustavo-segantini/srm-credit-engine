using SrmCreditEngine.Domain.Entities;
using SrmCreditEngine.Domain.Enums;

namespace SrmCreditEngine.Domain.Interfaces.Repositories;

public interface ICurrencyRepository : IRepository<Currency>
{
    Task<Currency?> GetByCodeAsync(CurrencyCode code, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Currency>> GetAllActiveAsync(CancellationToken cancellationToken = default);
}
