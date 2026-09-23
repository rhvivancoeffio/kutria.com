namespace Commerce.Application.Abstracts;

public interface IAccessTokenIssuer
{
    string Issue(string userId, string email, string tenantId, bool accountOwner);
}
