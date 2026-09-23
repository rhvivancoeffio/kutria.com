using System.Security.Cryptography;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace Commerce.Infrastructure.Auth;

/// <summary>
/// Builds HS256 signing keys. Microsoft.IdentityModel requires ≥256 bits;
/// short configured secrets are stretched with SHA-256.
/// </summary>
public static class AuthSigningKeys
{
    public static SymmetricSecurityKey CreateSymmetricKey(string secret)
    {
        var bytes = Encoding.UTF8.GetBytes(secret);
        if (bytes.Length < 32)
            bytes = SHA256.HashData(bytes);
        return new SymmetricSecurityKey(bytes);
    }
}
