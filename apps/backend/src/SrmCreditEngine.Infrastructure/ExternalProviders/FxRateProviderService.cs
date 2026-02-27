using System.Text.Json;
using Microsoft.Extensions.Logging;
using SrmCreditEngine.Application.Services;
using SrmCreditEngine.Domain.Enums;

namespace SrmCreditEngine.Infrastructure.ExternalProviders;

/// <summary>
/// Calls an external FX rate REST API (Open Exchange Rates or similar).
/// The <see cref="HttpClient"/> injected here is pre-configured with a
/// Polly resilience pipeline: retry (3x, exponential back-off) + circuit breaker.
///
/// For local/test environments the base address should be set to a mock URL
/// or left unconfigured — the fallback returns a hardcoded simulation value.
/// </summary>
public sealed class FxRateProviderService : IFxRateProviderService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<FxRateProviderService> _logger;

    public FxRateProviderService(
        HttpClient httpClient,
        ILogger<FxRateProviderService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<FxRateProviderResult?> FetchRateAsync(
        CurrencyCode from,
        CurrencyCode to,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Real endpoint pattern (Open Exchange Rates / Frankfurter / etc.)
            // Base address set in InfrastructureServiceExtensions.
            var url = $"latest?base={from}&symbols={to}";
            var response = await _httpClient.GetAsync(url, cancellationToken);
            response.EnsureSuccessStatusCode();

            using var doc = await JsonDocument.ParseAsync(
                await response.Content.ReadAsStreamAsync(cancellationToken),
                cancellationToken: cancellationToken);

            // Schema: { "rates": { "USD": 0.1234 } }  (Frankfurter-compatible)
            var rate = doc.RootElement
                .GetProperty("rates")
                .GetProperty(to.ToString())
                .GetDecimal();

            _logger.LogInformation(
                "Fetched FX rate {From}→{To} = {Rate} from external provider",
                from, to, rate);

            return new FxRateProviderResult(from, to, rate, "external-api", DateTime.UtcNow);
        }
        catch (HttpRequestException ex)
        {
            // Polly's circuit breaker will open after repeated failures.
            // We log and return null so the caller can fall back gracefully.
            _logger.LogWarning(ex,
                "External FX provider unavailable for {From}→{To}. Falling back to manual rates.",
                from, to);
            return null;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Unexpected error fetching FX rate {From}→{To}", from, to);
            return null;
        }
    }
}
