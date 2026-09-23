using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Commerce.Application.Abstracts;
using Commerce.Application.Features.CreateProduct;

namespace Commerce.Infrastructure.Agents.GravityCatalog;

internal static class GravityAgentContext
{
    public static async Task<(IGravityStoreDataClient Client, GravityStoreCredentials Credentials)> ResolveAsync(
        IServiceProvider services,
        CancellationToken cancellationToken)
    {
        var db = services.GetRequiredService<ICommerceDbContext>();
        var gravity = services.GetRequiredService<IGravityStoreDataClient>();
        var logger = services.GetRequiredService<ILoggerFactory>().CreateLogger("GravityAgentTools");
        var workspaceContext = services.GetRequiredService<IWorkspaceContext>();
        var workspaceId = workspaceContext.WorkspaceId
            ?? throw new InvalidOperationException("Workspace is required.");
        var (_, credentials) = await GravityTenantCredentials.LoadActiveAsync(
            db, logger, workspaceId, cancellationToken: cancellationToken);
        return (gravity, credentials);
    }

    public static string Json(object value)
        => JsonSerializer.Serialize(value, new JsonSerializerOptions(JsonSerializerDefaults.Web));
}
