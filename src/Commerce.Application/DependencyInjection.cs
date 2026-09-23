using System.Reflection;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Commerce.Application.Abstracts;
using Commerce.Application.Common.Behaviors;
using Commerce.Application.Features.DataIngestion;

namespace Commerce.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly()));
        services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
        var ingestedMemory = new IngestedCatalogMemory();
        services.AddSingleton<IIngestedCatalogMemory>(ingestedMemory);
        services.AddSingleton<ICatalogDataService>(new CompositeCatalogDataService(ingestedMemory));
        return services;
    }
}
