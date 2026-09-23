using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Commerce.Application.Abstracts;
using Commerce.Application.Abstracts.Mcp;
using Commerce.Domain.Mcp;

namespace Commerce.Infrastructure.Mcp;

public sealed class DbMcpOAuthStore(ICommerceDbContext db) : IMcpOAuthStore
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public async Task<OAuthClientRecord> RegisterClientAsync(OAuthClientRegistrationRequest request, CancellationToken ct = default)
    {
        string clientId;
        if (!string.IsNullOrEmpty(request.PreferredClientId))
        {
            var existing = await db.McpOAuthClients.AnyAsync(c => c.ClientId == request.PreferredClientId, ct);
            clientId = existing ? "mcp_" + Guid.NewGuid().ToString("N")[..16] : request.PreferredClientId;
        }
        else
        {
            clientId = "mcp_" + Guid.NewGuid().ToString("N")[..16];
        }

        var entity = new McpOAuthClient
        {
            ClientId = clientId,
            RedirectUrisJson = JsonSerializer.Serialize(request.RedirectUris, JsonOptions),
            GrantTypesJson = request.GrantTypes is null ? null : JsonSerializer.Serialize(request.GrantTypes, JsonOptions),
            ResponseTypesJson = request.ResponseTypes is null ? null : JsonSerializer.Serialize(request.ResponseTypes, JsonOptions),
            ClientName = request.ClientName,
            CreatedAt = DateTime.UtcNow
        };
        db.McpOAuthClients.Add(entity);
        await db.SaveChangesAsync(ct);

        return new OAuthClientRecord(
            clientId,
            request.RedirectUris,
            request.GrantTypes ?? ["authorization_code"],
            request.ResponseTypes ?? ["code"],
            null,
            request.ClientName);
    }

    public async Task<OAuthClientRecord?> GetClientAsync(string clientId, CancellationToken ct = default)
    {
        var entity = await db.McpOAuthClients.AsNoTracking()
            .FirstOrDefaultAsync(c => c.ClientId == clientId, ct);
        if (entity is null) return null;

        return new OAuthClientRecord(
            entity.ClientId,
            JsonSerializer.Deserialize<string[]>(entity.RedirectUrisJson) ?? [],
            string.IsNullOrEmpty(entity.GrantTypesJson)
                ? ["authorization_code"]
                : JsonSerializer.Deserialize<string[]>(entity.GrantTypesJson) ?? ["authorization_code"],
            string.IsNullOrEmpty(entity.ResponseTypesJson)
                ? ["code"]
                : JsonSerializer.Deserialize<string[]>(entity.ResponseTypesJson) ?? ["code"],
            null,
            entity.ClientName);
    }

    public async Task SaveAuthorizationCodeAsync(OAuthAuthorizationCodeRecord code, CancellationToken ct = default)
    {
        db.McpOAuthAuthorizationCodes.Add(new McpOAuthAuthorizationCode
        {
            Code = code.Code,
            ClientId = code.ClientId,
            RedirectUri = code.RedirectUri,
            CodeChallenge = code.CodeChallenge,
            CodeChallengeMethod = code.CodeChallengeMethod,
            Scope = code.Scope,
            Resource = code.Resource,
            UserId = code.UserId,
            TenantId = code.TenantId,
            ExpiresAt = code.ExpiresAt
        });
        await db.SaveChangesAsync(ct);
    }

    public async Task<OAuthAuthorizationCodeRecord?> ConsumeAuthorizationCodeAsync(
        string code,
        string clientId,
        CancellationToken ct = default)
    {
        var entity = await db.McpOAuthAuthorizationCodes
            .FirstOrDefaultAsync(c => c.Code == code && c.ClientId == clientId, ct);
        if (entity is null || entity.ExpiresAt < DateTime.UtcNow)
            return null;

        var result = new OAuthAuthorizationCodeRecord(
            entity.Code,
            entity.ClientId,
            entity.RedirectUri,
            entity.CodeChallenge,
            entity.CodeChallengeMethod,
            entity.Scope,
            entity.Resource,
            entity.UserId,
            entity.TenantId,
            entity.ExpiresAt);

        db.McpOAuthAuthorizationCodes.Remove(entity);
        await db.SaveChangesAsync(ct);
        return result;
    }

    public async Task SaveRefreshTokenAsync(OAuthRefreshTokenRecord refreshToken, CancellationToken ct = default)
    {
        db.McpOAuthRefreshTokens.Add(new McpOAuthRefreshToken
        {
            Token = refreshToken.Token,
            ClientId = refreshToken.ClientId,
            UserId = refreshToken.UserId,
            TenantId = refreshToken.TenantId,
            Scope = refreshToken.Scope,
            Resource = refreshToken.Resource,
            CreatedAt = refreshToken.CreatedAt,
            ExpiresAt = refreshToken.ExpiresAt
        });
        await db.SaveChangesAsync(ct);
    }

    public async Task<OAuthRefreshTokenRecord?> ConsumeRefreshTokenAsync(
        string refreshToken,
        string clientId,
        CancellationToken ct = default)
    {
        var entity = await db.McpOAuthRefreshTokens
            .FirstOrDefaultAsync(t => t.Token == refreshToken && t.ClientId == clientId, ct);
        if (entity is null || entity.ExpiresAt < DateTime.UtcNow)
            return null;

        var result = new OAuthRefreshTokenRecord(
            entity.Token,
            entity.ClientId,
            entity.UserId,
            entity.TenantId,
            entity.Scope,
            entity.Resource,
            entity.CreatedAt,
            entity.ExpiresAt);

        db.McpOAuthRefreshTokens.Remove(entity);
        await db.SaveChangesAsync(ct);
        return result;
    }
}
