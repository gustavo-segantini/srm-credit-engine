using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace SrmCreditEngine.IntegrationTests.Infrastructure;

/// <summary>
/// Extension methods shared across integration test classes.
/// </summary>
public static class TestHttpClientExtensions
{
    private sealed record TokenResponse(string AccessToken, int ExpiresIn, string TokenType, DateTime IssuedAt);

    /// <summary>
    /// Calls /api/v1/auth/token and sets the Authorization: Bearer header on
    /// the <paramref name="client"/> so subsequent requests are authenticated.
    /// </summary>
    public static async Task AuthenticateAsync(this HttpClient client, string username = "test-operator")
    {
        var response = await client.PostAsJsonAsync("/api/v1/auth/token",
            new { username, role = "operator", tenant = "srm-default" });

        response.EnsureSuccessStatusCode();
        var token = await response.Content.ReadFromJsonAsync<TokenResponse>();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token!.AccessToken);
    }
}
