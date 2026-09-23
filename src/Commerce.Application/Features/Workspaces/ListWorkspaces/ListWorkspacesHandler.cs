using Microsoft.EntityFrameworkCore;
using Commerce.Application.Abstracts;
using Commerce.Application.Common.Abstracts;

namespace Commerce.Application.Features.Workspaces.ListWorkspaces;

public sealed class ListWorkspacesHandler(ICommerceDbContext db)
    : IQueryHandler<ListWorkspacesQuery, ListWorkspacesResult>
{
    public async Task<ListWorkspacesResult> Handle(ListWorkspacesQuery request, CancellationToken cancellationToken)
    {
        var items = await db.Workspaces
            .OrderBy(w => w.EnvironmentKind)
            .ThenBy(w => w.Name)
            .Select(w => new WorkspaceListItem(
                w.Id,
                w.Name,
                w.IsDefault,
                w.IsSystem,
                w.EnvironmentKind.ToString(),
                w.CreatedAt))
            .ToListAsync(cancellationToken);

        return new ListWorkspacesResult(items);
    }
}
