using Carter;
using FluentValidation;
using MediatR;
using Commerce.Application.Features.Auth.CheckTenantAvailability;
using Commerce.Application.Features.Auth.GetMe;
using Commerce.Application.Features.Auth.SignIn;
using Commerce.Application.Features.Auth.SignUp;

namespace Commerce.Api.Modules;

public sealed class AuthModule : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        var auth = app.MapGroup("/auth").WithTags("Auth");
        auth.MapGet("/config", () => Results.Ok(new { useCentralIdp = false }));
        auth.MapGet("/tenants/available", async (string identifier, IMediator mediator, CancellationToken ct) =>
            Results.Ok(await mediator.Send(new CheckTenantAvailabilityQuery(identifier), ct)));
        auth.MapPost("/signup", async (SignUpRequest body, IMediator mediator, CancellationToken ct) =>
            await Send(mediator, new SignUpCommand(
                body.Identifier,
                body.Email,
                body.Password,
                body.DisplayName,
                body.Name,
                body.PlanKey), ct));
        auth.MapPost("/signin", async (SignInRequest body, IMediator mediator, CancellationToken ct) =>
            await Send(mediator, new SignInCommand(body.Email, body.Password), ct));

        var byTenant = app.MapGroup("/t/{__tenant__}/auth").WithTags("Auth");
        byTenant.MapPost("/signin", async (SignInRequest body, IMediator mediator, CancellationToken ct) =>
            await Send(mediator, new SignInCommand(body.Email, body.Password), ct));
        byTenant.MapGet("/me", async (HttpRequest http, IMediator mediator, CancellationToken ct) =>
            {
                var result = await Send(mediator, new GetMeQuery(ExtractBearer(http)), ct);
                if (result is IStatusCodeHttpResult { StatusCode: >= 200 and < 300 })
                {
                    http.HttpContext.Response.Headers.CacheControl = "no-store, no-cache, must-revalidate";
                    http.HttpContext.Response.Headers.Pragma = "no-cache";
                    http.HttpContext.Response.Headers.Expires = "0";
                    http.HttpContext.Response.Headers.Append("Vary", "Authorization");
                }
                return result;
            });
    }

    private static string ExtractBearer(HttpRequest http)
    {
        var header = http.Headers.Authorization.ToString();
        if (string.IsNullOrWhiteSpace(header))
            return string.Empty;
        const string prefix = "Bearer ";
        return header.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)
            ? header[prefix.Length..].Trim()
            : header.Trim();
    }

    private static async Task<IResult> Send<T>(IMediator mediator, IRequest<T> request, CancellationToken ct)
    {
        try
        {
            return Results.Ok(await mediator.Send(request, ct));
        }
        catch (ValidationException ex)
        {
            var message = ex.Errors.FirstOrDefault()?.ErrorMessage ?? "Datos inválidos.";
            return Results.BadRequest(new { error = message });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Results.Json(new { error = ex.Message }, statusCode: StatusCodes.Status401Unauthorized);
        }
        catch (InvalidOperationException ex)
        {
            return Results.Conflict(new { error = ex.Message });
        }
    }

    private sealed record SignUpRequest(
        string Identifier,
        string Email,
        string Password,
        string DisplayName,
        string? Name,
        string? PlanKey);

    private sealed record SignInRequest(string Email, string Password);
}
