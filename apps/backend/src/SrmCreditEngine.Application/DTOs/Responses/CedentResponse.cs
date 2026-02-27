namespace SrmCreditEngine.Application.DTOs.Responses;

public sealed record CedentResponse(
    Guid Id,
    string Name,
    string Cnpj,
    string? ContactEmail,
    bool IsActive,
    DateTime CreatedAt,
    DateTime UpdatedAt);
