using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Commerce.Application.Abstracts;
using Commerce.Application.Features.ApiKeys.CreateApiKey;
using Commerce.Infrastructure.Persistence;
using Finbuckle.MultiTenant.Abstractions;

namespace Commerce.Mcp.Middleware;

/// <summary>
/// Authenticates MCP requests via <c>x-api-key</c> when no Bearer token is present.
/// Sets Finbuckle tenant from the key's TenantId.
/// </summary>
public sealed class ApiKeyAuthenticationMiddleware(RequestDelegate next)
{
    public const string HeaderName = "x-api-key";

    public async Task InvokeAsync(HttpContext context)
    {
        if (context.User.Identity?.IsAuthenticated == true)
        {
            await next(context);
            return;
        }

        var auth = context.Request.Headers.Authorization.FirstOrDefault();
        if (!string.IsNullOrEmpty(auth) && auth.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            await next(context);
            return;
        }

        if (!context.Request.Headers.TryGetValue(HeaderName, out var keyValues))
        {
            await next(context);
            return;
        }

        var raw = keyValues.ToString();
        if (string.IsNullOrWhiteSpace(raw))
        {
            await next(context);
            return;
        }

        var hash = CreateApiKeyHandler.Sha256Hex(raw);
        var db = context.RequestServices.GetRequiredService<ICommerceDbContext>();
        var apiKey = await db.TenantApiKeys
            .IgnoreQueryFilters()
            .AsNoTracking()
            .FirstOrDefaultAsync(k => k.KeyHash == hash && k.IsActive && k.RevokedAt == null);

        if (apiKey is null)
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsJsonAsync(new { error = "invalid_api_key" });
            return;
        }

        var tenantStore = context.RequestServices.GetRequiredService<ITenantStore>();
        var tenant = await tenantStore.GetByIdAsync(apiKey.TenantId)
            ?? await tenantStore.GetByIdentifierAsync(apiKey.TenantId);

        if (tenant is null)
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsJsonAsync(new { error = "tenant_not_found" });
            return;
        }

        TenantBootstrap.SetCurrentTenant(context.RequestServices, tenant);

        var identity = new ClaimsIdentity("ApiKey");
        identity.AddClaim(new Claim("tenant_id", tenant.Id));
        identity.AddClaim(new Claim("tenant_identifier", tenant.Identifier));
        if (apiKey.WorkspaceId is Guid workspaceId)
            identity.AddClaim(new Claim("workspace_id", workspaceId.ToString("D")));
        identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, apiKey.Id.ToString()));
        context.User = new ClaimsPrincipal(identity);

        // Fire-and-forget last used update (best effort)
        _ = UpdateLastUsedAsync(context.RequestServices, apiKey.Id);

        await next(context);
    }

    private static async Task UpdateLastUsedAsync(IServiceProvider root, Guid id)
    {
        try
        {
            await using var scope = root.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<ICommerceDbContext>();
            var entity = await db.TenantApiKeys.IgnoreQueryFilters().FirstOrDefaultAsync(k => k.Id == id);
            if (entity is null) return;
            entity.LastUsedAt = DateTimeOffset.UtcNow;
            await db.SaveChangesAsync();
        }
        catch
        {
            // ignore
        }
    }
}
