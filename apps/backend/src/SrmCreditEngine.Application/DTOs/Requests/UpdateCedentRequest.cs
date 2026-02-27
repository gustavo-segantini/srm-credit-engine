namespace SrmCreditEngine.Application.DTOs.Requests;

/// <summary>Request to update an existing cedent's mutable fields.</summary>
public sealed record UpdateCedentRequest(
    string Name,
    string? ContactEmail);
