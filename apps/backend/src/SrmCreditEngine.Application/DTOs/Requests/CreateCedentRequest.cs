namespace SrmCreditEngine.Application.DTOs.Requests;

/// <summary>Request to onboard a new cedent (company that cedes receivables).</summary>
public sealed record CreateCedentRequest(
    string Name,
    string Cnpj,
    string? ContactEmail);
