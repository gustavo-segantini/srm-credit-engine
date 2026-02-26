using SrmCreditEngine.Domain.Exceptions;

namespace SrmCreditEngine.Domain.Entities;


/// <summary>
/// Represents a company (cedente) that cedes receivables to the fund.
/// </summary>
public sealed class Cedent : Entity
{
    public string Name { get; private set; }

    /// <summary>CNPJ — stored as digits only (14 chars), validated at application layer.</summary>
    public string Cnpj { get; private set; }

    public string? ContactEmail { get; private set; }
    public bool IsActive { get; private set; }
    public uint RowVersion { get; private set; }

    // Navigation
    private readonly List<Receivable> _receivables = [];
    public IReadOnlyCollection<Receivable> Receivables => _receivables.AsReadOnly();

    private Cedent() { }  // EF Core

    public Cedent(string name, string cnpj, string? contactEmail = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new BusinessRuleViolationException("INVALID_CEDENT", "Cedent name cannot be empty.");

        if (string.IsNullOrWhiteSpace(cnpj) || cnpj.Length != 14)
            throw new BusinessRuleViolationException("INVALID_CNPJ", "CNPJ must contain exactly 14 digits.");

        Name = name;
        Cnpj = cnpj;
        ContactEmail = contactEmail;
        IsActive = true;
    }

    public void Update(string name, string? contactEmail)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new BusinessRuleViolationException("INVALID_CEDENT", "Cedent name cannot be empty.");

        Name = name;
        ContactEmail = contactEmail;
        TouchUpdatedAt();
    }

    public void Deactivate()
    {
        IsActive = false;
        TouchUpdatedAt();
    }
}
