using Commerce.Application.Common.Abstracts;

namespace Commerce.Application.Features.Tenants.CreateTenant;

public sealed record CreateTenantCommand(string Identifier, string Name) : ICommand<CreateTenantResult>;
