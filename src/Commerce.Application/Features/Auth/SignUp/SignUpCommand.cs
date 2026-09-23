using Commerce.Application.Common.Abstracts;

namespace Commerce.Application.Features.Auth.SignUp;

public sealed record SignUpCommand(
    string Identifier,
    string Email,
    string Password,
    string DisplayName,
    string? Name,
    string? PlanKey) : ICommand<SignUpResult>;
