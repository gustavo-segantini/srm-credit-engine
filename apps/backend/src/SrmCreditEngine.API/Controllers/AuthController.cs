using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;

namespace SrmCreditEngine.API.Controllers;

/// <summary>
/// Minimal auth endpoint for development and integration testing.
/// In production this would delegate to an Identity Provider (Keycloak, Azure AD B2C, etc.).
/// </summary>
[ApiController]
[Route("api/v1/auth")]
[Produces("application/json")]
[AllowAnonymous]
public sealed class AuthController : ControllerBase
{
    private readonly IConfiguration _configuration;

    public AuthController(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    /// <summary>
    /// Issues a JWT Bearer token for a given username/role.
    /// ⚠️ Development only — replace with a real IdP in production.
    /// </summary>
    [HttpPost("token")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public IActionResult Token([FromBody] TokenRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Username))
            return BadRequest(new { message = "Username is required." });

        var jwtSettings = _configuration.GetSection("Jwt");
        var secretKey = jwtSettings["SecretKey"]
            ?? throw new InvalidOperationException("JWT SecretKey not configured.");

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, request.Username),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(ClaimTypes.Role, request.Role ?? "operator"),
            new("tenant", request.Tenant ?? "srm-default"),
        };

        var expiresInMinutes = int.Parse(jwtSettings["ExpiresInMinutes"] ?? "60");

        var token = new JwtSecurityToken(
            issuer: jwtSettings["Issuer"],
            audience: jwtSettings["Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(expiresInMinutes),
            signingCredentials: credentials);

        return Ok(new
        {
            accessToken = new JwtSecurityTokenHandler().WriteToken(token),
            expiresIn = expiresInMinutes * 60,
            tokenType = "Bearer",
            issuedAt = DateTime.UtcNow,
        });
    }
}

public sealed record TokenRequest(string Username, string? Role, string? Tenant);
