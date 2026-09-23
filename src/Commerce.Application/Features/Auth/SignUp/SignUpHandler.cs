using Microsoft.EntityFrameworkCore;
using Commerce.Application.Abstracts;
using Commerce.Application.Common.Abstracts;
using Commerce.Domain.Auth;
using Commerce.Domain.Tenants;

namespace Commerce.Application.Features.Auth.SignUp;

public sealed class SignUpHandler(
    ITenantStore tenants,
    ITenantProvisioner provisioner,
    ICommerceDbContext db,
    IPasswordHasher passwords)
    : ICommandHandler<SignUpCommand, SignUpResult>
{
    public async Task<SignUpResult> Handle(SignUpCommand request, CancellationToken cancellationToken)
    {
        var shopName = request.Identifier.Trim();
        var identifier = TenantIdentifierRules.Slugify(shopName);
        var email = request.Email.Trim().ToLowerInvariant();
        var displayName = request.DisplayName.Trim();
        var name = string.IsNullOrWhiteSpace(request.Name) ? shopName : request.Name.Trim();

        if (await tenants.GetByIdentifierAsync(identifier, cancellationToken) is not null)
        {
            throw new InvalidOperationException("Ese identificador ya está en uso.");
        }

        if (await db.TenantOwners.AnyAsync(o => o.Email == email, cancellationToken))
        {
            throw new InvalidOperationException("Ya existe una cuenta con ese correo.");
        }

        var tenant = await tenants.AddAsync(new CommerceTenantInfo
        {
            Id = identifier,
            Identifier = identifier,
            Name = name
        }, cancellationToken);

        var plan = string.IsNullOrWhiteSpace(request.PlanKey) ? "free" : request.PlanKey.Trim();
        await provisioner.EnsureDefaultsAsync(tenant, plan, cancellationToken);

        db.TenantOwners.Add(new TenantOwner
        {
            TenantId = tenant.Id,
            Email = email,
            DisplayName = displayName,
            PasswordHash = passwords.Hash(request.Password)
        });
        await db.SaveChangesAsync(cancellationToken);

        return new SignUpResult(tenant.Identifier, tenant.Name);
    }
}
