using SrmCreditEngine.Application.DTOs.Requests;
using SrmCreditEngine.Application.DTOs.Responses;
using SrmCreditEngine.Application.Interfaces;
using SrmCreditEngine.Domain.Entities;
using SrmCreditEngine.Domain.Exceptions;
using SrmCreditEngine.Domain.Interfaces.Repositories;

namespace SrmCreditEngine.Application.Services;

public sealed class CedentService : ICedentService
{
    private readonly ICedentRepository _cedentRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CedentService(ICedentRepository cedentRepository, IUnitOfWork unitOfWork)
    {
        _cedentRepository = cedentRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<CedentResponse> CreateAsync(
        CreateCedentRequest request,
        CancellationToken cancellationToken = default)
    {
        var cnpjDigits = new string(request.Cnpj.Where(char.IsDigit).ToArray());

        if (await _cedentRepository.ExistsByCnpjAsync(cnpjDigits, cancellationToken))
            throw new BusinessRuleViolationException(
                "CNPJ_DUPLICATE",
                $"A cedent with CNPJ {cnpjDigits} already exists.");

        var cedent = new Cedent(request.Name, cnpjDigits, request.ContactEmail);
        await _cedentRepository.AddAsync(cedent, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return MapToResponse(cedent);
    }

    public async Task<CedentResponse> UpdateAsync(
        Guid id,
        UpdateCedentRequest request,
        CancellationToken cancellationToken = default)
    {
        var cedent = await _cedentRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new BusinessRuleViolationException("CEDENT_NOT_FOUND", $"Cedent {id} not found.");

        cedent.Update(request.Name, request.ContactEmail);
        _cedentRepository.Update(cedent);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return MapToResponse(cedent);
    }

    public async Task DeactivateAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var cedent = await _cedentRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new BusinessRuleViolationException("CEDENT_NOT_FOUND", $"Cedent {id} not found.");

        cedent.Deactivate();
        _cedentRepository.Update(cedent);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task<CedentResponse?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var cedent = await _cedentRepository.GetByIdAsync(id, cancellationToken);
        return cedent is null ? null : MapToResponse(cedent);
    }

    public async Task<IReadOnlyList<CedentResponse>> GetAllActiveAsync(
        CancellationToken cancellationToken = default)
    {
        var cedents = await _cedentRepository.GetAllActiveAsync(cancellationToken);
        return cedents.Select(MapToResponse).ToList();
    }

    private static CedentResponse MapToResponse(Cedent c) =>
        new(c.Id, c.Name, c.Cnpj, c.ContactEmail, c.IsActive, c.CreatedAt, c.UpdatedAt);
}
