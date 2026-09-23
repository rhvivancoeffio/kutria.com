using Commerce.Application.Common.Abstracts;

namespace Commerce.Application.Features.Tenants.UpdateTenant;

public sealed record UpdateTenantCommand(string Identifier, string Name) : ICommand<UpdateTenantResult>;
