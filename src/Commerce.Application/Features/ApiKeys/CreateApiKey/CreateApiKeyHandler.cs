using System.Security.Cryptography;
using System.Text;
using Finbuckle.MultiTenant.Abstractions;
using Commerce.Application.Abstracts;
using Commerce.Application.Common;
using Commerce.Application.Common.Abstracts;
using Commerce.Domain.ApiKeys;
using Commerce.Domain.Tenants;

namespace Commerce.Application.Features.ApiKeys.CreateApiKey;

public sealed class CreateApiKeyHandler(
    ICommerceDbContext db,
    IMultiTenantContextAccessor<CommerceTenantInfo> tenantAccessor,
    IWorkspaceContext workspaceContext)
    : ICommandHandler<CreateApiKeyCommand, CreateApiKeyResult>
{
    public async Task<CreateApiKeyResult> Handle(CreateApiKeyCommand request, CancellationToken cancellationToken)
    {
        var tenant = tenantAccessor.MultiTenantContext?.TenantInfo
            ?? throw new InvalidOperationException("Tenant is required.");
        var workspaceId = await WorkspaceRequirement.RequireWorkspaceIdAsync(
            workspaceContext, db, cancellationToken);

        var raw = "ck_" + Convert.ToHexString(RandomNumberGenerator.GetBytes(24)).ToLowerInvariant();
        var prefix = raw[..12];
        var entity = new TenantApiKey
        {
            TenantId = tenant.Id!,
            WorkspaceId = workspaceId,
            Name = request.Name.Trim(),
            KeyPrefix = prefix,
            KeyHash = Sha256Hex(raw),
            IsActive = true
        };
        db.TenantApiKeys.Add(entity);
        await db.SaveChangesAsync(cancellationToken);

        return new CreateApiKeyResult(entity.Id, entity.Name, entity.KeyPrefix, raw, entity.CreatedAt);
    }

    public static string Sha256Hex(string value)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(value));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}
