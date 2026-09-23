using FluentValidation;
using Commerce.Application.Abstracts;

namespace Commerce.Application.Features.Brains.ProcessBrainIngest;

public sealed class ProcessBrainIngestCommandValidator : AbstractValidator<ProcessBrainIngestCommand>
{
    public ProcessBrainIngestCommandValidator()
    {
        RuleFor(x => x.JobId).NotEmpty();
        RuleFor(x => x.Step).Must(step =>
                string.Equals(step, BrainIngestMessage.Parse, StringComparison.Ordinal)
                || string.Equals(step, BrainIngestMessage.Publish, StringComparison.Ordinal))
            .WithMessage("Step must be parse or publish.");
    }
}
