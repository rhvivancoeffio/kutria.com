using FluentValidation;

namespace Commerce.Application.Features.Workspaces.CreateWorkspace;

public sealed class CreateWorkspaceCommandValidator : AbstractValidator<CreateWorkspaceCommand>
{
    public CreateWorkspaceCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.EnvironmentKind).IsInEnum();
    }
}
