namespace SrmCreditEngine.Domain.Exceptions;

public sealed class InvalidPricingException(string reason) : DomainException("INVALID_PRICING", $"Cannot price receivable: {reason}")
{
}
