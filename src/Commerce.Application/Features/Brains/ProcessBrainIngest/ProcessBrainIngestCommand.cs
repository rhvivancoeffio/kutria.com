using Commerce.Application.Common.Abstracts;

namespace Commerce.Application.Features.Brains.ProcessBrainIngest;

public sealed record ProcessBrainIngestCommand(Guid JobId, string Step) : ICommand<ProcessBrainIngestResult>;
