using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Commerce.Application.Abstracts.Mcp;

namespace Commerce.Infrastructure.Auth;

/// <summary>Validates tokens issued by <see cref="HmacAccessTokenIssuer"/> (SignIn/SignUp).</summary>
public sealed class HmacAccessTokenValidator(IConfiguration configuration) : IAccessTokenValidator
{
    public AccessTokenPrincipal? Validate(string token)
    {
        var parts = token.Split('.');
        if (parts.Length != 3)
            return null;

        var secret = configuration["Auth:SigningKey"];
        if (string.IsNullOrWhiteSpace(secret))
            secret = "kutria-dev-signing-key";

        var signingInput = $"{parts[0]}.{parts[1]}";
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var expected = Base64Url(hmac.ComputeHash(Encoding.UTF8.GetBytes(signingInput)));
        if (!CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(expected),
                Encoding.UTF8.GetBytes(parts[2])))
        {
            return null;
        }

        try
        {
            var json = Encoding.UTF8.GetString(Base64UrlDecode(parts[1]));
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            if (root.TryGetProperty("exp", out var exp) &&
                exp.TryGetInt64(out var expUnix) &&
                DateTimeOffset.UtcNow.ToUnixTimeSeconds() > expUnix)
            {
                return null;
            }

            var userId = root.TryGetProperty("sub", out var sub) ? sub.GetString() : null;
            var email = root.TryGetProperty("email", out var em) ? em.GetString() : null;
            var tenantId = root.TryGetProperty("tenant_id", out var tid) ? tid.GetString() : null;
            var owner = root.TryGetProperty("account_owner", out var ao) &&
                        string.Equals(ao.GetString(), "true", StringComparison.OrdinalIgnoreCase);

            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(tenantId))
                return null;

            return new AccessTokenPrincipal(userId, email ?? "", tenantId, owner);
        }
        catch
        {
            return null;
        }
    }

    private static string Base64Url(byte[] bytes)
        => Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    private static byte[] Base64UrlDecode(string input)
    {
        var s = input.Replace('-', '+').Replace('_', '/');
        switch (s.Length % 4)
        {
            case 2: s += "=="; break;
            case 3: s += "="; break;
        }
        return Convert.FromBase64String(s);
    }
}
