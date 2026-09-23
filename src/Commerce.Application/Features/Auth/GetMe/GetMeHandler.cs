using Finbuckle.MultiTenant.Abstractions;
using Microsoft.EntityFrameworkCore;
using Commerce.Application.Abstracts;
using Commerce.Application.Abstracts.Mcp;
using Commerce.Application.Common.Abstracts;
using Commerce.Application.Features.Integrations;
using Commerce.Domain.Tenants;

namespace Commerce.Application.Features.Auth.GetMe;

public sealed class GetMeHandler(
    IAccessTokenValidator tokens,
    ICommerceDbContext db,
    ITenantStore tenants,
    IMultiTenantContextAccessor<CommerceTenantInfo> tenantAccessor)
    : IQueryHandler<GetMeQuery, GetMeResult>
{
    public async Task<GetMeResult> Handle(GetMeQuery request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.AccessToken))
            throw new UnauthorizedAccessException("Se requiere autenticación.");

        var principal = tokens.Validate(request.AccessToken.Trim())
            ?? throw new UnauthorizedAccessException("Token inválido o expirado.");

        var resolvedTenant = tenantAccessor.MultiTenantContext?.TenantInfo;
        if (resolvedTenant is not null &&
            !string.Equals(principal.TenantId, resolvedTenant.Id, StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(principal.TenantId, resolvedTenant.Identifier, StringComparison.OrdinalIgnoreCase))
        {
            throw new UnauthorizedAccessException("El token no pertenece a este tenant.");
        }

        if (!Guid.TryParse(principal.UserId, out var userId))
            throw new UnauthorizedAccessException("Token inválido.");

        var owner = await db.TenantOwners
            .AsNoTracking()
            .FirstOrDefaultAsync(o => o.Id == userId, cancellationToken);

        if (owner is null)
            throw new UnauthorizedAccessException("Usuario no encontrado.");

        if (!string.Equals(owner.TenantId, principal.TenantId, StringComparison.OrdinalIgnoreCase))
            throw new UnauthorizedAccessException("Usuario no pertenece a este tenant.");

        var mode = await ResolveStoreModeAsync(owner.TenantId, resolvedTenant, cancellationToken);

        return new GetMeResult(
            owner.Id,
            owner.Email,
            owner.DisplayName,
            owner.CreatedAt,
            HasPassword: !string.IsNullOrWhiteSpace(owner.PasswordHash),
            IsAccountOwner: principal.AccountOwner,
            TenantId: owner.TenantId,
            CommerceStoreMode: mode.ToString());
    }

    private async Task<CommerceStoreMode> ResolveStoreModeAsync(
        string tenantId,
        CommerceTenantInfo? accessorTenant,
        CancellationToken cancellationToken)
    {
        var stored = await tenants.GetByIdAsync(tenantId, cancellationToken)
            ?? await tenants.GetByIdentifierAsync(tenantId, cancellationToken);

        if (stored is null)
            return accessorTenant?.CommerceStoreMode ?? CommerceStoreMode.None;

        if (stored.CommerceStoreMode != CommerceStoreMode.None)
        {
            if (accessorTenant is not null)
                accessorTenant.CommerceStoreMode = stored.CommerceStoreMode;
            return stored.CommerceStoreMode;
        }

        // One-time backfill for tenants that already had integrations before this column existed.
        var providers = await db.Integrations
            .AsNoTracking()
            .Select(i => i.Provider)
            .ToListAsync(cancellationToken);

        if (providers.Count == 0)
            return CommerceStoreMode.None;

        var inferred = providers.Any(p =>
                string.Equals(p, TenantCommerceStoreModeUpdater.NativeGravityProvider, StringComparison.OrdinalIgnoreCase))
            ? CommerceStoreMode.Native
            : CommerceStoreMode.Connected;

        stored.CommerceStoreMode = inferred;
        await tenants.UpdateAsync(stored, cancellationToken);
        if (accessorTenant is not null)
            accessorTenant.CommerceStoreMode = inferred;
        return inferred;
    }
}
