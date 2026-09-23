namespace Commerce.Application.Features.CreateProduct;

public interface IOnboardingImageStore
{
    Task SaveAsync(
        string tenantId,
        string workflowId,
        byte[] bytes,
        string contentType,
        CancellationToken cancellationToken = default);

    Task<(byte[] Bytes, string ContentType)?> GetAsync(
        string tenantId,
        string workflowId,
        CancellationToken cancellationToken = default);
}
