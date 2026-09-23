using Microsoft.Extensions.DependencyInjection;
using Commerce.Application.Abstracts;

namespace Commerce.Infrastructure.Vectors;

public static class VectorIndexBootstrap
{
    public static async Task EnsureVectorIndexesAsync(
        this IServiceProvider services,
        CancellationToken cancellationToken = default)
    {
        var bootstrapper = services.GetService<IVectorIndexBootstrapper>();
        if (bootstrapper is null)
            return;

        await bootstrapper.EnsureAsync(cancellationToken);
    }
}
