namespace SrmCreditEngine.Domain.Exceptions;

public sealed class ExchangeRateNotFoundException(string fromCurrency, string toCurrency) : DomainException("EXCHANGE_RATE_NOT_FOUND",
        $"Exchange rate from {fromCurrency} to {toCurrency} was not found or is expired.")
{
}
