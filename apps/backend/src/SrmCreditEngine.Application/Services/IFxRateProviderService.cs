using SrmCreditEngine.Domain.Enums;

namespace SrmCreditEngine.Application.Services;

/// <summary>
/// Contract for an external FX rate data provider.
/// Implementations are expected to call a third-party REST API
/// and MUST be resilient (retry + circuit breaker via Polly).
/// </summary>
public interface IFxRateProviderService
{
    /// <summary>
    /// Fetches the current exchange rate from an external provider.
    /// Returns null when the rate is unavailable or the provider is down.
    /// </summary>
    Task<FxRateProviderResult?> FetchRateAsync(
        CurrencyCode from,
        CurrencyCode to,
        CancellationToken cancellationToken = default);
}

public sealed record FxRateProviderResult(
    CurrencyCode From,
    CurrencyCode To,
    decimal Rate,
    string Source,
    DateTime FetchedAt);
