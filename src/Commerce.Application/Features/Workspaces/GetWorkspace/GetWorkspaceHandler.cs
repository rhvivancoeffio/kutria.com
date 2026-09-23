using Microsoft.EntityFrameworkCore;
using Commerce.Application.Abstracts;
using Commerce.Application.Common.Abstracts;

namespace Commerce.Application.Features.Workspaces.GetWorkspace;

public sealed class GetWorkspaceHandler(ICommerceDbContext db)
    : IQueryHandler<GetWorkspaceQuery, GetWorkspaceResult?>
{
    public async Task<GetWorkspaceResult?> Handle(GetWorkspaceQuery request, CancellationToken cancellationToken)
    {
        return await db.Workspaces
            .Where(w => w.Id == request.Id)
            .Select(w => new GetWorkspaceResult(
                w.Id,
                w.Name,
                w.IsDefault,
                w.IsSystem,
                w.EnvironmentKind.ToString(),
                w.CreatedAt))
            .FirstOrDefaultAsync(cancellationToken);
    }
}
