using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using SrmCreditEngine.IntegrationTests.Infrastructure;

namespace SrmCreditEngine.IntegrationTests.Endpoints;

/// <summary>
/// Integration tests for POST /api/v1/auth/token
/// </summary>
public sealed class AuthEndpointTests : IClassFixture<SrmCreditEngineFactory>
{
    private readonly HttpClient _client;

    public AuthEndpointTests(SrmCreditEngineFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Token_WithUsername_Returns200WithAccessToken()
    {
        // Arrange — any username yields a token (dev-only IdP)
        var body = new { username = "operator", role = "operator", tenant = "srm-default" };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/auth/token", body);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = await response.Content.ReadFromJsonAsync<TokenResponse>();
        json.Should().NotBeNull();
        json!.AccessToken.Should().NotBeNullOrWhiteSpace();
        json.TokenType.Should().Be("Bearer");
    }

    [Fact]
    public async Task Token_EmptyUsername_Returns400()
    {
        // Arrange
        var body = new { username = "", role = (string?)null, tenant = (string?)null };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/auth/token", body);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // Response DTO
    private sealed record TokenResponse(
        string AccessToken,
        int ExpiresIn,
        string TokenType,
        DateTime IssuedAt);
}
