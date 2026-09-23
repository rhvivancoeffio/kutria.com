using System.Security.Claims;
using System.Text.Json;
using Commerce.Application.Abstracts;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Commerce.Infrastructure.Accounts;

/// <summary>
/// Resolves workspace for HTTP (header / JWT claim) or for background hosts via
/// <see cref="WorkspaceBootstrap"/> ambient (AsyncLocal). Does not require
/// <see cref="IHttpContextAccessor"/> — Worker hosts without HTTP still resolve.
/// </summary>
public sealed class WorkspaceContext : IWorkspaceContext
{
    private static readonly AsyncLocal<Guid?> AmbientWorkspaceId = new();

    private readonly IHttpContextAccessor? _httpContextAccessor;

    public WorkspaceContext(IHttpContextAccessor? httpContextAccessor = null)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public Guid? WorkspaceId
    {
        get
        {
            if (AmbientWorkspaceId.Value is Guid ambient)
                return ambient;

            var http = _httpContextAccessor?.HttpContext;
            if (http is null)
                return null;

            var user = http.User;
            var isApiKey = string.Equals(user?.Identity?.AuthenticationType, "ApiKey", StringComparison.OrdinalIgnoreCase);

            if (!isApiKey)
            {
                var header = http.Request.Headers["X-Workspace-Id"].FirstOrDefault();
                if (Guid.TryParse(header, out var fromHeader))
                    return fromHeader;
            }

            var claim = user?.FindFirst("workspace_id")?.Value;
            return Guid.TryParse(claim, out var fromClaim) ? fromClaim : null;
        }
    }

    internal static void SetAmbient(Guid? workspaceId) => AmbientWorkspaceId.Value = workspaceId;

    internal static Guid? GetAmbient() => AmbientWorkspaceId.Value;
}

/// <summary>
/// Sets the ambient workspace for the current async flow (Worker queues, workflows).
/// Mirrors <c>TenantBootstrap</c> for Finbuckle tenant context.
/// </summary>
public static class WorkspaceBootstrap
{
    public static void SetCurrentWorkspace(Guid? workspaceId)
        => WorkspaceContext.SetAmbient(workspaceId);

    /// <summary>
    /// Sets ambient workspace. Restores the previous value on dispose.
    /// </summary>
    public static IDisposable Use(Guid? workspaceId)
    {
        var previous = WorkspaceContext.GetAmbient();
        WorkspaceContext.SetAmbient(workspaceId);
        return new RestoreAmbient(previous);
    }

    public static async Task<IDisposable> UseFromBrainJobAsync(
        IServiceProvider services,
        Guid jobId,
        CancellationToken cancellationToken = default)
    {
        var db = services.GetRequiredService<ICommerceDbContext>();
        var workspaceId = await db.BrainIngestJobs.AsNoTracking()
            .Where(x => x.Id == jobId)
            .Select(x => x.WorkspaceId)
            .FirstOrDefaultAsync(cancellationToken);
        return Use(workspaceId);
    }

    public static async Task<IDisposable> UseFromIntegrationAsync(
        IServiceProvider services,
        Guid integrationId,
        CancellationToken cancellationToken = default)
    {
        var db = services.GetRequiredService<ICommerceDbContext>();
        var workspaceId = await db.Integrations.AsNoTracking()
            .Where(x => x.Id == integrationId)
            .Select(x => x.WorkspaceId)
            .FirstOrDefaultAsync(cancellationToken);
        return Use(workspaceId);
    }

    /// <summary>
    /// Tries <c>workspaceId</c> / <c>WorkspaceId</c> from a JSON workflow payload.
    /// </summary>
    public static IDisposable UseFromJsonPayload(string payload)
    {
        Guid? workspaceId = null;
        try
        {
            using var doc = JsonDocument.Parse(string.IsNullOrWhiteSpace(payload) ? "{}" : payload);
            var root = doc.RootElement;
            if (root.ValueKind == JsonValueKind.Object)
            {
                if (root.TryGetProperty("workspaceId", out var a) && a.ValueKind == JsonValueKind.String
                    && Guid.TryParse(a.GetString(), out var fromCamel))
                    workspaceId = fromCamel;
                else if (root.TryGetProperty("WorkspaceId", out var b) && b.ValueKind == JsonValueKind.String
                         && Guid.TryParse(b.GetString(), out var fromPascal))
                    workspaceId = fromPascal;
            }
        }
        catch (JsonException)
        {
            /* ignore malformed payload */
        }

        return Use(workspaceId);
    }

    private sealed class RestoreAmbient(Guid? previous) : IDisposable
    {
        public void Dispose() => WorkspaceContext.SetAmbient(previous);
    }
}
