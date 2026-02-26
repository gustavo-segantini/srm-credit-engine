using SrmCreditEngine.Domain.Enums;
using SrmCreditEngine.Domain.Exceptions;
using SrmCreditEngine.Domain.ValueObjects;

namespace SrmCreditEngine.Domain.Entities;

/// <summary>
/// Represents a financial receivable (duplicata, cheque pré-datado, etc.)
/// submitted for pricing and settlement to the fund.
/// </summary>
public sealed class Receivable : Entity
{
    public Guid CedentId { get; private set; }
    public string DocumentNumber { get; private set; }
    public ReceivableType Type { get; private set; }
    public decimal FaceValue { get; private set; }
    public CurrencyCode FaceCurrency { get; private set; }
    public DateTime DueDate { get; private set; }
    public DateTime SubmittedAt { get; private set; }

    // Navigation
    public Cedent Cedent { get; private set; } = null!;
    public Settlement? Settlement { get; private set; }

    private Receivable() { }  // EF Core

    public Receivable(
        Guid cedentId,
        string documentNumber,
        ReceivableType type,
        decimal faceValue,
        CurrencyCode faceCurrency,
        DateTime dueDate)
    {
        if (faceValue <= 0)
            throw new InvalidPricingException("Face value must be greater than zero.");

        if (dueDate <= DateTime.UtcNow.Date)
            throw new InvalidPricingException("Due date must be in the future.");

        if (string.IsNullOrWhiteSpace(documentNumber))
            throw new BusinessRuleViolationException("INVALID_DOC", "Document number cannot be empty.");

        CedentId = cedentId;
        DocumentNumber = documentNumber;
        Type = type;
        FaceValue = faceValue;
        FaceCurrency = faceCurrency;
        DueDate = dueDate.ToUniversalTime();
        SubmittedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Calculates term in months (rounded up) from today to due date.
    /// </summary>
    public int GetTermInMonths(DateTime fromDate)
    {
        var diff = (DueDate - fromDate.ToUniversalTime());
        var months = (int)Math.Ceiling(diff.TotalDays / 30.0);
        return Math.Max(1, months);
    }
}
