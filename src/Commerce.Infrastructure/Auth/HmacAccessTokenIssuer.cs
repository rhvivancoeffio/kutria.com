using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Commerce.Application.Abstracts;

namespace Commerce.Infrastructure.Auth;

public sealed class HmacAccessTokenIssuer(IConfiguration configuration) : IAccessTokenIssuer
{
    public string Issue(string userId, string email, string tenantId, bool accountOwner)
    {
        var secret = configuration["Auth:SigningKey"];
        if (string.IsNullOrWhiteSpace(secret))
        {
            secret = "kutria-dev-signing-key";
        }

        var header = Base64Url("{\"alg\":\"HS256\",\"typ\":\"JWT\"}");
        var payload = Base64Url(JsonSerializer.Serialize(new Dictionary<string, object>
        {
            ["sub"] = userId,
            ["email"] = email,
            ["tenant_id"] = tenantId,
            ["account_owner"] = accountOwner ? "true" : "false",
            ["exp"] = DateTimeOffset.UtcNow.AddDays(7).ToUnixTimeSeconds()
        }));
        var signature = Base64Url(Sign($"{header}.{payload}", secret));
        return $"{header}.{payload}.{signature}";
    }

    private static byte[] Sign(string value, string secret)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        return hmac.ComputeHash(Encoding.UTF8.GetBytes(value));
    }

    private static string Base64Url(string value) => Base64Url(Encoding.UTF8.GetBytes(value));

    private static string Base64Url(byte[] bytes)
        => Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
}
