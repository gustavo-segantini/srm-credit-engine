namespace SrmCreditEngine.Domain.Exceptions;

public sealed class InvalidPricingException : DomainException
{
    public InvalidPricingException(string reason)
        : base("INVALID_PRICING", $"Cannot price receivable: {reason}")
    {
    }
}
