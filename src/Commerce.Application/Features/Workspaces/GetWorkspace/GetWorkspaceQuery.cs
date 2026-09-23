using Commerce.Application.Common.Abstracts;

namespace Commerce.Application.Features.Workspaces.GetWorkspace;

public sealed record GetWorkspaceQuery(Guid Id) : IQuery<GetWorkspaceResult?>;
