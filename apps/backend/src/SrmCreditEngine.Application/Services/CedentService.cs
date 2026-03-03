using SrmCreditEngine.Application.DTOs.Requests;
using SrmCreditEngine.Application.DTOs.Responses;
using SrmCreditEngine.Application.Interfaces;
using SrmCreditEngine.Domain.Entities;
using SrmCreditEngine.Domain.Exceptions;
using SrmCreditEngine.Domain.Interfaces.Repositories;

namespace SrmCreditEngine.Application.Services;

public sealed class CedentService(ICedentRepository cedentRepository, IUnitOfWork unitOfWork) : ICedentService
{
    public async Task<CedentResponse> CreateAsync(
        CreateCedentRequest request,
        CancellationToken cancellationToken = default)
    {
        var cnpjDigits = new string(request.Cnpj.Where(char.IsDigit).ToArray());

        if (await cedentRepository.ExistsByCnpjAsync(cnpjDigits, cancellationToken))
        {
            throw new BusinessRuleViolationException(
                "CNPJ_DUPLICATE",
                $"A cedent with CNPJ {cnpjDigits} already exists.");
        }

        var cedent = new Cedent(request.Name, cnpjDigits, request.ContactEmail);
        await cedentRepository.AddAsync(cedent, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return MapToResponse(cedent);
    }

    public async Task<CedentResponse> UpdateAsync(
        Guid id,
        UpdateCedentRequest request,
        CancellationToken cancellationToken = default)
    {
        var cedent = await cedentRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new BusinessRuleViolationException("CEDENT_NOT_FOUND", $"Cedent {id} not found.");

        cedent.Update(request.Name, request.ContactEmail);
        cedentRepository.Update(cedent);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return MapToResponse(cedent);
    }

    public async Task DeactivateAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var cedent = await cedentRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new BusinessRuleViolationException("CEDENT_NOT_FOUND", $"Cedent {id} not found.");

        cedent.Deactivate();
        cedentRepository.Update(cedent);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task<CedentResponse?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var cedent = await cedentRepository.GetByIdAsync(id, cancellationToken);
        return cedent is null ? null : MapToResponse(cedent);
    }

    public async Task<IReadOnlyList<CedentResponse>> GetAllActiveAsync(
        CancellationToken cancellationToken = default)
    {
        var cedents = await cedentRepository.GetAllActiveAsync(cancellationToken);
        return cedents.Select(MapToResponse).ToList();
    }

    private static CedentResponse MapToResponse(Cedent c) =>
        new(c.Id, c.Name, c.Cnpj, c.ContactEmail, c.IsActive, c.CreatedAt, c.UpdatedAt);
}
