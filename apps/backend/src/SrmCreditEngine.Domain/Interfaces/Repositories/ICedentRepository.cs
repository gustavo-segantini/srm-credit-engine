using SrmCreditEngine.Domain.Entities;

namespace SrmCreditEngine.Domain.Interfaces.Repositories;

public interface ICedentRepository : IRepository<Cedent>
{
    Task<Cedent?> GetByCnpjAsync(string cnpj, CancellationToken cancellationToken = default);
    Task<bool> ExistsByCnpjAsync(string cnpj, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Cedent>> GetAllActiveAsync(CancellationToken cancellationToken = default);
}
