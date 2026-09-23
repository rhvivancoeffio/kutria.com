using Microsoft.EntityFrameworkCore;
using Commerce.Application.Abstracts;
using Commerce.Application.Common.Abstracts;

namespace Commerce.Application.Features.Auth.SignIn;

public sealed class SignInHandler(
    ICommerceDbContext db,
    IPasswordHasher passwords,
    IAccessTokenIssuer tokens)
    : ICommandHandler<SignInCommand, SignInResult>
{
    public async Task<SignInResult> Handle(SignInCommand request, CancellationToken cancellationToken)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var owner = await db.TenantOwners
            .FirstOrDefaultAsync(o => o.Email == email, cancellationToken);

        if (owner is null || !passwords.Verify(request.Password, owner.PasswordHash))
        {
            throw new UnauthorizedAccessException("Correo o contraseña incorrectos.");
        }

        var token = tokens.Issue(owner.Id.ToString(), owner.Email, owner.TenantId, accountOwner: true);
        return new SignInResult(token, owner.TenantId);
    }
}
