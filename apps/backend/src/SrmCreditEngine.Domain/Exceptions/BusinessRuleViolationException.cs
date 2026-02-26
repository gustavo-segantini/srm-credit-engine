namespace SrmCreditEngine.Domain.Exceptions;

/// <summary>
/// General-purpose domain exception for business rule violations not covered by more specific exceptions.
/// </summary>
public sealed class BusinessRuleViolationException : DomainException
{
    public BusinessRuleViolationException(string code, string message)
        : base(code, message)
    {
    }
}
