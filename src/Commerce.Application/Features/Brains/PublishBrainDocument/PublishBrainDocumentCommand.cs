using Commerce.Application.Common.Abstracts;

namespace Commerce.Application.Features.Brains.PublishBrainDocument;

public sealed record PublishBrainDocumentCommand(Guid JobId, string? EditedText) : ICommand<PublishBrainDocumentResult>;
