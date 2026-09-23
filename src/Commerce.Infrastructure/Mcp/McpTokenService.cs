using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Commerce.Application.Abstracts.Mcp;
using Commerce.Application.Configuration;
using Commerce.Infrastructure.Auth;

namespace Commerce.Infrastructure.Mcp;

public sealed class McpTokenService(IConfiguration configuration, IOptions<McpOptions> mcpOptions) : IMcpTokenService
{
    public string GenerateAccessToken(
        string userId,
        string email,
        string tenantId,
        string[] scopes,
        string mcpResourceUri)
    {
        var secret = configuration["Auth:SigningKey"];
        if (string.IsNullOrWhiteSpace(secret))
            secret = "kutria-dev-signing-key";

        var opts = mcpOptions.Value;
        var issuer = string.IsNullOrWhiteSpace(opts.PublicBaseUrl)
            ? "https://mcp.kutria.com"
            : opts.PublicBaseUrl.TrimEnd('/');
        var key = AuthSigningKeys.CreateSymmetricKey(secret);
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId),
            new(JwtRegisteredClaimNames.Sub, userId),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new("tenant_id", tenantId),
            new("scope", string.Join(" ", scopes))
        };
        if (!string.IsNullOrEmpty(email))
        {
            claims.Add(new Claim(ClaimTypes.Email, email));
            claims.Add(new Claim(JwtRegisteredClaimNames.Email, email));
        }

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: mcpResourceUri.TrimEnd('/'),
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(opts.AccessTokenExpirationMinutes),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
