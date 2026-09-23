namespace Commerce.Application.Abstracts;

public interface ISessionBuyerProfileStore
{
    Task<SessionBuyerProfile> GetAsync(
        string tenantId,
        string threadId,
        CancellationToken cancellationToken = default);

    Task SaveAsync(
        SessionBuyerProfile profile,
        CancellationToken cancellationToken = default);
}
