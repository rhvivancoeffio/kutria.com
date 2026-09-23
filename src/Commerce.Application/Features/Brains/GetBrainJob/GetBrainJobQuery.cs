using Commerce.Application.Common.Abstracts;

namespace Commerce.Application.Features.Brains.GetBrainJob;

public sealed record GetBrainJobQuery(Guid JobId) : IQuery<GetBrainJobResult?>;
