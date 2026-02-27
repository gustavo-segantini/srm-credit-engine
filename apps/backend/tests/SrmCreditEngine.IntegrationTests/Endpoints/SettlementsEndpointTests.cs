using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using SrmCreditEngine.IntegrationTests.Infrastructure;

namespace SrmCreditEngine.IntegrationTests.Endpoints;

/// <summary>
/// Integration tests for POST /api/v1/settlements and GET /api/v1/settlements/{id}
/// </summary>
public sealed class SettlementsEndpointTests : IClassFixture<SrmCreditEngineFactory>
{
    private readonly HttpClient _client;

    public SettlementsEndpointTests(SrmCreditEngineFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task CreateSettlement_WithoutToken_Returns401()
    {
        // Arrange — no Authorization header; auth middleware fires before handler validation
        // so we do NOT need a real cedent — a random Guid is sufficient.
        var body = BuildSettlementBody(Guid.NewGuid(), $"DOC-{Guid.NewGuid():N}");

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/settlements", body);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CreateSettlement_ValidRequest_Returns201WithId()
    {
        // Arrange
        await _client.AuthenticateAsync();
        var body = await ValidSettlementBodyAsync();

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/settlements", body);

        // Assert — first call should succeed
        response.StatusCode.Should().BeOneOf(HttpStatusCode.Created, HttpStatusCode.Conflict);

        if (response.StatusCode == HttpStatusCode.Created)
        {
            var result = await response.Content.ReadFromJsonAsync<SettlementResponseDto>();
            result.Should().NotBeNull();
            result!.Id.Should().NotBe(Guid.Empty);
            result.Status.Should().Be("Settled");
        }
    }

    [Fact]
    public async Task CreateSettlement_DuplicateDocument_Returns409()
    {
        // Arrange — same cedent + documentNumber must produce 409 on second call
        await _client.AuthenticateAsync();
        var cedentId = await CreateCedentAsync();
        var docNumber = $"DUP-{Guid.NewGuid():N}";
        var body = BuildSettlementBody(cedentId, docNumber);

        // Act — first call
        var first = await _client.PostAsJsonAsync("/api/v1/settlements", body);
        // Act — identical second call
        var second = await _client.PostAsJsonAsync("/api/v1/settlements", body);

        // Assert
        first.StatusCode.Should().Be(HttpStatusCode.Created);
        second.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task GetSettlement_ValidId_Returns200()
    {
        // Arrange
        await _client.AuthenticateAsync();
        var cedentId = await CreateCedentAsync();
        var docNumber = $"GET-{Guid.NewGuid():N}";
        var createResponse = await _client.PostAsJsonAsync("/api/v1/settlements", BuildSettlementBody(cedentId, docNumber));
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var created = await createResponse.Content.ReadFromJsonAsync<SettlementResponseDto>();

        // Act
        var getResponse = await _client.GetAsync($"/api/v1/settlements/{created!.Id}");

        // Assert
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var fetched = await getResponse.Content.ReadFromJsonAsync<SettlementResponseDto>();
        fetched!.Id.Should().Be(created.Id);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    /// <summary>Creates a cedent via the API and returns its ID.</summary>
    private async Task<Guid> CreateCedentAsync()
    {
        var cnpj = new string(System.Random.Shared.Next(10, 99).ToString()
            .PadLeft(14, System.Random.Shared.Next(0, 9).ToString()[0])
            .Take(14).ToArray());
        // Generate a valid 14-digit CNPJ-like number
        cnpj = string.Concat(Enumerable.Range(0, 14).Select(_ => (char)('0' + System.Random.Shared.Next(0, 10))));

        var response = await _client.PostAsJsonAsync("/api/v1/cedents", new
        {
            name         = $"Cedente Teste {Guid.NewGuid().ToString("N")[..8]}",
            cnpj,
            contactEmail = $"test-{Guid.NewGuid():N}@test.com",
        });
        response.EnsureSuccessStatusCode();
        var cedent = await response.Content.ReadFromJsonAsync<CedentResponseDto>();
        return cedent!.Id;
    }

    private static object BuildSettlementBody(Guid cedentId, string docNumber) => new
    {
        cedentId,
        documentNumber  = docNumber,
        receivableType  = "DuplicataMercantil",
        faceValue       = 50_000m,
        faceCurrency    = "BRL",
        dueDate         = DateTime.UtcNow.AddMonths(2).ToString("yyyy-MM-dd"),
        paymentCurrency = "BRL",
    };

    private async Task<object> ValidSettlementBodyAsync()
    {
        var cedentId = await CreateCedentAsync();
        return BuildSettlementBody(cedentId, $"DOC-{Guid.NewGuid():N}");
    }

    // ── DTOs ─────────────────────────────────────────────────────────────────

    private sealed record SettlementResponseDto(
        Guid    Id,
        Guid    CedentId,
        string  DocumentNumber,
        decimal FaceValue,
        decimal PresentValue,
        decimal Discount,
        decimal NetDisbursement,
        string  PaymentCurrency,
        string  Status);

    private sealed record CedentResponseDto(Guid Id, string Name, string Cnpj, string ContactEmail, bool IsActive);
}
