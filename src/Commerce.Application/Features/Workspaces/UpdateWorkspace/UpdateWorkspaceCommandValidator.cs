using FluentValidation;

namespace Commerce.Application.Features.Workspaces.UpdateWorkspace;

public sealed class UpdateWorkspaceCommandValidator : AbstractValidator<UpdateWorkspaceCommand>
{
    public UpdateWorkspaceCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.EnvironmentKind).IsInEnum();
    }
}
