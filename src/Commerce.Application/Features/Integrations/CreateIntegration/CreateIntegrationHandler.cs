using System.Text.Json;
using Finbuckle.MultiTenant.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Commerce.Application.Abstracts;
using Commerce.Application.Common;
using Commerce.Application.Common.Abstracts;
using Commerce.Application.Features.Integrations;
using Commerce.Domain.Integrations;
using Commerce.Domain.Tenants;

namespace Commerce.Application.Features.Integrations.CreateIntegration;

public sealed class CreateIntegrationHandler(
    ICommerceDbContext db,
    IMultiTenantContextAccessor<CommerceTenantInfo> tenantAccessor,
    IWorkspaceContext workspaceContext,
    ITenantStore tenants,
    IIntegrationsMetadataService metadataService,
    IMeetGravityStoreProvisioner meetGravityProvisioner,
    IGravityStoreDataClient gravityStore,
    ILogger<CreateIntegrationHandler> logger)
    : ICommandHandler<CreateIntegrationCommand, CreateIntegrationResult>
{
    public async Task<CreateIntegrationResult> Handle(
        CreateIntegrationCommand request,
        CancellationToken cancellationToken)
    {
        var tenant = tenantAccessor.MultiTenantContext?.TenantInfo
            ?? throw new InvalidOperationException("Tenant is required.");
        var workspaceId = await WorkspaceRequirement.RequireWorkspaceIdAsync(
            workspaceContext, db, cancellationToken);

        var meta = await metadataService.GetByProviderAsync(request.Provider, cancellationToken)
            ?? throw new InvalidOperationException($"Unknown integration provider '{request.Provider}'.");

        if (!meta.AvailableForConnect)
            throw new InvalidOperationException($"Provider '{meta.Key}' is not available to connect.");

        //await EnforcePlanLimitAsync(cancellationToken);

        var settings = IntegrationSettingsPayloadNormalizer.ToStringDictionary(meta, request.Settings);

        if (meetGravityProvisioner.SupportsProvider(meta.Key))
        {
            var provisioned = await ResolveMeetGravitySettingsAsync(tenant, meta.Key, settings, cancellationToken);
            foreach (var kv in provisioned)
                settings[kv.Key] = kv.Value;
        }

        if (GravityDefaultSellerBootstrap.IsGravityProvider(meta.Key))
        {
            await GravityDefaultSellerBootstrap.EnrichSettingsWithDefaultSellerAsync(
                    settings,
                    gravityStore,
                    logger,
                    cancellationToken)
                .ConfigureAwait(false);
        }

        var settingsJson = JsonSerializer.Serialize(settings);

        var integration = new Integration
        {
            TenantId = tenant.Id!,
            WorkspaceId = workspaceId,
            Provider = meta.Key,
            Name = request.Name.Trim(),
            SettingsJson = settingsJson,
            IsActive = true
        };

        db.Integrations.Add(integration);
        await db.SaveChangesAsync(cancellationToken);

        await ApplyCommerceStoreModeAsync(tenant, meta.Key, cancellationToken);

        return new CreateIntegrationResult(
            integration.Id,
            integration.WorkspaceId,
            integration.Provider,
            integration.Name,
            integration.IsActive,
            integration.CreatedAt);
    }

    private async Task ApplyCommerceStoreModeAsync(
        CommerceTenantInfo accessorTenant,
        string provider,
        CancellationToken cancellationToken)
    {
        var stored = await tenants.GetByIdAsync(accessorTenant.Id, cancellationToken)
            ?? await tenants.GetByIdentifierAsync(accessorTenant.Identifier, cancellationToken);
        if (stored is null)
        {
            logger.LogWarning(
                "Could not load tenant {TenantId} to update CommerceStoreMode after creating {Provider}.",
                accessorTenant.Id,
                provider);
            return;
        }

        var next = TenantCommerceStoreModeUpdater.ResolveNextMode(stored.CommerceStoreMode, provider);
        if (next == stored.CommerceStoreMode)
            return;

        stored.CommerceStoreMode = next;
        await tenants.UpdateAsync(stored, cancellationToken);
        accessorTenant.CommerceStoreMode = next;
        logger.LogInformation(
            "Tenant {TenantId} CommerceStoreMode set to {Mode} after provider {Provider}.",
            stored.Id,
            next,
            provider);
    }

    private async Task<IReadOnlyDictionary<string, string>> ResolveMeetGravitySettingsAsync(
        CommerceTenantInfo tenant,
        string provider,
        IReadOnlyDictionary<string, string> userSettings,
        CancellationToken cancellationToken)
    {
        var existing = await TryFindExistingMeetGravityCredentialsAsync(provider, cancellationToken);

        var ownerDisplayName = await db.TenantOwners
            .AsNoTracking()
            .Where(o => o.TenantId == tenant.Id)
            .Select(o => o.DisplayName)
            .FirstOrDefaultAsync(cancellationToken);

        return await meetGravityProvisioner.ProvisionAsync(
            tenant.Id!,
            tenant.Name ?? tenant.Identifier ?? tenant.Id!,
            ownerDisplayName,
            provider,
            userSettings,
            existing?.ClientId,
            existing?.ClientSecret,
            cancellationToken);
    }

    private async Task<(string ClientId, string ClientSecret)?> TryFindExistingMeetGravityCredentialsAsync(
        string provider,
        CancellationToken cancellationToken)
    {
        var rows = await db.Integrations
            .AsNoTracking()
            .Where(i => i.Provider == provider)
            .Select(i => i.SettingsJson)
            .ToListAsync(cancellationToken);

        foreach (var json in rows)
        {
            if (string.IsNullOrWhiteSpace(json))
                continue;

            Dictionary<string, string>? parsed;
            try
            {
                parsed = JsonSerializer.Deserialize<Dictionary<string, string>>(json);
            }
            catch (JsonException)
            {
                continue;
            }

            if (parsed is null)
                continue;

            if (!parsed.TryGetValue(MeetGravitySettingKeys.ClientId, out var clientId)
                || string.IsNullOrWhiteSpace(clientId))
                continue;

            if (!parsed.TryGetValue(MeetGravitySettingKeys.ClientSecret, out var clientSecret)
                || string.IsNullOrWhiteSpace(clientSecret))
                continue;

            return (clientId, clientSecret);
        }

        return null;
    }

    private async Task EnforcePlanLimitAsync(CancellationToken cancellationToken)
    {
        var plan = await db.TenantBillings.Select(b => b.PlanCode).FirstOrDefaultAsync(cancellationToken) ?? "free";
        var max = plan switch
        {
            "pro" => 50,
            "starter" => 5,
            _ => 1
        };

        var count = await db.Integrations.CountAsync(cancellationToken);
        if (count >= max)
        {
            throw new InvalidOperationException(
                $"Plan '{plan}' allows at most {max} integration(s). Upgrade billing to add more.");
        }
    }
}
