using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using SrmCreditEngine.IntegrationTests.Infrastructure;

namespace SrmCreditEngine.IntegrationTests.Endpoints;

/// <summary>
/// Integration tests for POST /api/v1/pricing/simulate
/// </summary>
public sealed class PricingEndpointTests : IClassFixture<SrmCreditEngineFactory>
{
    private readonly HttpClient _client;

    public PricingEndpointTests(SrmCreditEngineFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Simulate_WithoutToken_Returns401()
    {
        // Arrange — no Authorization header
        var body = ValidSimulateBody();

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/pricing/simulate", body);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Simulate_ValidRequest_Returns200WithPricingResult()
    {
        // Arrange
        await _client.AuthenticateAsync();
        var body = ValidSimulateBody();

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/pricing/simulate", body);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<PricingSimResponseDto>();
        result.Should().NotBeNull();
        result!.PresentValue.Should().BeGreaterThan(0);
        result.NetDisbursement.Should().BeGreaterThan(0);
        result.Discount.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task Simulate_FaceValueZero_Returns400()
    {
        // Arrange
        await _client.AuthenticateAsync();
        var body = new
        {
            faceValue      = 0m,
            faceCurrency   = "BRL",
            receivableType = "DuplicataMercantil",
            dueDate        = DateTime.UtcNow.AddMonths(3).ToString("yyyy-MM-dd"),
            paymentCurrency = "BRL",
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/pricing/simulate", body);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static object ValidSimulateBody() => new
    {
        faceValue      = 100_000m,
        faceCurrency   = "BRL",
        receivableType = "DuplicataMercantil",
        dueDate        = DateTime.UtcNow.AddMonths(3).ToString("yyyy-MM-dd"),
        paymentCurrency = "BRL",
    };

    private sealed record PricingSimResponseDto(
        decimal FaceValue,
        decimal PresentValue,
        decimal Discount,
        decimal NetDisbursement,
        decimal BaseRate,
        decimal Spread,
        decimal TermInMonths,
        string  PaymentCurrency);
}
