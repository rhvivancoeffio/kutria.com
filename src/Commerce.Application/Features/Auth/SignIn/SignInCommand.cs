using Commerce.Application.Common.Abstracts;

namespace Commerce.Application.Features.Auth.SignIn;

public sealed record SignInCommand(string Email, string Password) : ICommand<SignInResult>;
