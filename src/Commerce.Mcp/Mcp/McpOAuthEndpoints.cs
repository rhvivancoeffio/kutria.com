using System.Text.Json;
using System.Text.Json.Serialization;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Commerce.Application.Abstracts.Mcp;
using Commerce.Application.Configuration;
using Commerce.Application.Features.McpOAuth;
using Commerce.Application.Features.McpOAuth.Authorize;
using Commerce.Application.Features.McpOAuth.AuthorizeComplete;
using Commerce.Application.Features.McpOAuth.GetAuthorizationServerMetadata;
using Commerce.Application.Features.McpOAuth.GetProtectedResourceMetadata;
using Commerce.Application.Features.McpOAuth.Register;
using Commerce.Application.Features.McpOAuth.Token;
using Commerce.Infrastructure.Mcp;

namespace Commerce.Mcp.Mcp;

public static class McpOAuthEndpoints
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = null };

    public static void MapMcpOAuthEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet(OAuthWellKnownPaths.OAuthProtectedResource, GetProtectedResourceMetadata)
            .AllowAnonymous();
        app.MapGet(OAuthWellKnownPaths.OAuthAuthorizationServer, GetAuthorizationServerMetadata)
            .AllowAnonymous();

        var oauth = app.MapGroup("/oauth").WithTags("MCP OAuth");
        oauth.MapGet("/authorize", Authorize).AllowAnonymous();
        oauth.MapPost("/authorize/complete", AuthorizeComplete).AllowAnonymous();
        oauth.MapPost("/token", Token).AllowAnonymous();
        oauth.MapPost("/register", Register).AllowAnonymous();
    }

    private static async Task<IResult> GetProtectedResourceMetadata(
        HttpContext context,
        IMediator mediator,
        IOptions<McpOptions> options)
    {
        var baseUrl = GetPublicBaseUrl(context, options.Value);
        var metadata = await mediator.Send(
            new McpOAuthGetProtectedResourceMetadataQuery(baseUrl, options.Value.FrontendBaseUrl));
        return Results.Json(new
        {
            resource = metadata.Resource,
            authorization_servers = metadata.AuthorizationServers,
            scopes_supported = metadata.ScopesSupported,
            resource_documentation = metadata.ResourceDocumentation
        }, JsonOptions);
    }

    private static async Task<IResult> GetAuthorizationServerMetadata(
        HttpContext context,
        IMediator mediator,
        IOptions<McpOptions> options)
    {
        var baseUrl = GetPublicBaseUrl(context, options.Value);
        var metadata = await mediator.Send(new McpOAuthGetAuthorizationServerMetadataQuery(baseUrl));
        return Results.Json(new
        {
            issuer = metadata.Issuer,
            authorization_endpoint = metadata.AuthorizationEndpoint,
            token_endpoint = metadata.TokenEndpoint,
            registration_endpoint = metadata.RegistrationEndpoint,
            code_challenge_methods_supported = metadata.CodeChallengeMethodsSupported,
            scopes_supported = metadata.ScopesSupported,
            response_types_supported = metadata.ResponseTypesSupported,
            grant_types_supported = metadata.GrantTypesSupported,
            // Public clients (ChatGPT, Claude, Postman, Cursor) use PKCE without client_secret.
            token_endpoint_auth_methods_supported = new[] { "none" },
            registration_endpoint_auth_methods_supported = new[] { "none" }
        }, JsonOptions);
    }

    private static async Task<IResult> Authorize(
        [FromQuery] string? client_id,
        [FromQuery] string? redirect_uri,
        [FromQuery] string? response_type,
        [FromQuery] string? scope,
        [FromQuery] string? state,
        [FromQuery] string? code_challenge,
        [FromQuery] string? code_challenge_method,
        [FromQuery] string? resource,
        IMediator mediator,
        ILoggerFactory loggerFactory)
    {
        var logger = loggerFactory.CreateLogger("Commerce.Mcp.OAuth");
        logger.LogInformation(
            "OAuth authorize start client_id={ClientId} redirect_uri={RedirectUri} state_len={StateLen} challenge_method={Method} resource={Resource}",
            client_id,
            redirect_uri,
            state?.Length ?? 0,
            code_challenge_method,
            resource);
        try
        {
            var result = await mediator.Send(new McpOAuthAuthorizeQuery(
                client_id ?? "",
                redirect_uri ?? "",
                response_type,
                scope,
                state,
                code_challenge,
                code_challenge_method,
                resource));
            logger.LogInformation("OAuth authorize redirecting to consent UI");
            return Results.Redirect(result.RedirectUrl);
        }
        catch (McpOAuthException ex)
        {
            logger.LogWarning(
                "OAuth authorize rejected: {Error} — {Description} (client_id={ClientId}, redirect_uri={RedirectUri})",
                ex.ErrorCode, ex.ErrorDescription, client_id, redirect_uri);
            return Results.BadRequest(new { error = ex.ErrorCode, error_description = ex.ErrorDescription });
        }
    }

    private static async Task<IResult> AuthorizeComplete(
        HttpContext context,
        [FromBody] AuthorizeCompleteRequest body,
        IMediator mediator,
        IOptions<McpOptions> options,
        IAccessTokenValidator tokenValidator,
        ILoggerFactory loggerFactory)
    {
        var logger = loggerFactory.CreateLogger("Commerce.Mcp.OAuth");
        var authHeader = context.Request.Headers.Authorization.FirstOrDefault();
        if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            logger.LogWarning("OAuth authorize/complete missing Bearer (user must be signed in on Web)");
            return Results.Json(new { error = "invalid_request", error_description = "Authorization Bearer required" }, statusCode: 401);
        }

        var raw = authHeader["Bearer ".Length..].Trim();
        var principal = tokenValidator.Validate(raw);
        if (principal is null)
        {
            logger.LogWarning("OAuth authorize/complete invalid user session token");
            return Results.Json(new { error = "invalid_request", error_description = "Valid session required. Please sign in first." }, statusCode: 401);
        }

        try
        {
            var publicBase = GetPublicBaseUrl(context, options.Value);
            var result = await mediator.Send(new McpOAuthAuthorizeCompleteCommand(
                principal.UserId,
                principal.TenantId,
                body.ClientId,
                body.RedirectUri,
                body.State,
                body.CodeChallenge,
                body.CodeChallengeMethod,
                body.Scope,
                body.Resource,
                body.ClientName,
                publicBase));
            logger.LogInformation(
                "OAuth authorize/complete OK tenant={TenantId} client_id={ClientId} redirect_host={Host} → client will call /oauth/token next",
                principal.TenantId,
                body.ClientId,
                SafeHost(body.RedirectUri));
            return Results.Ok(new { redirectUrl = result.RedirectUrl });
        }
        catch (McpOAuthException ex)
        {
            logger.LogWarning(
                "OAuth authorize/complete rejected: {Error} — {Description} (client_id={ClientId})",
                ex.ErrorCode, ex.ErrorDescription, body.ClientId);
            return Results.BadRequest(new { error = ex.ErrorCode, error_description = ex.ErrorDescription });
        }
    }

    private static async Task<IResult> Token(
        HttpContext context,
        IMediator mediator,
        IOptions<McpOptions> options,
        ILoggerFactory loggerFactory)
    {
        var logger = loggerFactory.CreateLogger("Commerce.Mcp.OAuth");
        var contentType = context.Request.ContentType ?? "";
        logger.LogInformation(
            "OAuth token HIT Content-Type={ContentType} ContentLength={Length}",
            contentType,
            context.Request.ContentLength);

        string? grantType = null;
        string? code = null;
        string? redirectUri = null;
        string? clientId = null;
        string? codeVerifier = null;
        string? resource = null;
        string? refreshToken = null;

        try
        {
            if (contentType.Contains("application/json", StringComparison.OrdinalIgnoreCase))
            {
                using var doc = await JsonDocument.ParseAsync(context.Request.Body);
                var root = doc.RootElement;
                grantType = GetJsonString(root, "grant_type");
                code = GetJsonString(root, "code");
                redirectUri = GetJsonString(root, "redirect_uri");
                clientId = GetJsonString(root, "client_id");
                codeVerifier = GetJsonString(root, "code_verifier");
                resource = GetJsonString(root, "resource");
                refreshToken = GetJsonString(root, "refresh_token");
            }
            else
            {
                var form = await context.Request.ReadFormAsync();
                grantType = form["grant_type"];
                code = form["code"];
                redirectUri = form["redirect_uri"];
                clientId = form["client_id"];
                codeVerifier = form["code_verifier"];
                resource = form["resource"];
                refreshToken = form["refresh_token"];
            }

            // Optional Basic client_id:secret (public clients often send empty secret).
            if (string.IsNullOrEmpty(clientId)
                && context.Request.Headers.Authorization.FirstOrDefault() is { } auth
                && auth.StartsWith("Basic ", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    var decoded = System.Text.Encoding.UTF8.GetString(
                        Convert.FromBase64String(auth["Basic ".Length..].Trim()));
                    var colon = decoded.IndexOf(':');
                    clientId = colon >= 0 ? decoded[..colon] : decoded;
                    logger.LogInformation("OAuth token client_id from Basic auth");
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "OAuth token failed to parse Basic Authorization");
                }
            }

            logger.LogInformation(
                "OAuth token params grant_type={GrantType} client_id={ClientId} code_present={HasCode} code_len={CodeLen} verifier_present={HasVerifier} redirect_uri={RedirectUri} resource={Resource} refresh_present={HasRefresh}",
                grantType,
                clientId,
                !string.IsNullOrEmpty(code),
                code?.Length ?? 0,
                !string.IsNullOrEmpty(codeVerifier),
                redirectUri,
                resource,
                !string.IsNullOrEmpty(refreshToken));

            var publicBase = GetPublicBaseUrl(context, options.Value);
            var result = await mediator.Send(new McpOAuthTokenCommand(
                grantType ?? "",
                code ?? "",
                redirectUri,
                clientId ?? "",
                codeVerifier,
                resource,
                publicBase,
                refreshToken));

            logger.LogInformation(
                "OAuth token SUCCESS grant_type={GrantType} client_id={ClientId} scope={Scope} expires_in={ExpiresIn}",
                grantType,
                clientId,
                result.Scope,
                result.ExpiresIn);

            var response = new Dictionary<string, object>
            {
                ["access_token"] = result.AccessToken,
                ["token_type"] = result.TokenType,
                ["expires_in"] = result.ExpiresIn,
                ["scope"] = result.Scope
            };
            if (!string.IsNullOrEmpty(result.RefreshToken))
            {
                response["refresh_token"] = result.RefreshToken!;
                if (result.RefreshTokenExpiresIn.HasValue)
                    response["refresh_token_expires_in"] = result.RefreshTokenExpiresIn.Value;
            }
            return Results.Json(response, JsonOptions);
        }
        catch (McpOAuthException ex)
        {
            logger.LogWarning(
                "OAuth token rejected: {Error} — {Description} (grant_type={GrantType}, client_id={ClientId}, code_present={HasCode})",
                ex.ErrorCode, ex.ErrorDescription, grantType, clientId, !string.IsNullOrEmpty(code));
            return Results.BadRequest(new { error = ex.ErrorCode, error_description = ex.ErrorDescription });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "OAuth token unexpected failure (grant_type={GrantType}, client_id={ClientId})", grantType, clientId);
            return Results.BadRequest(new { error = "server_error", error_description = "Token endpoint failed" });
        }
    }

    private static string? GetJsonString(JsonElement root, string name)
        => root.TryGetProperty(name, out var el) && el.ValueKind == JsonValueKind.String
            ? el.GetString()
            : null;

    private static string SafeHost(string? uri)
    {
        try
        {
            return string.IsNullOrEmpty(uri) ? "" : new Uri(uri).Host;
        }
        catch
        {
            return uri ?? "";
        }
    }

    private static async Task<IResult> Register(
        [FromBody] DcrRequest body,
        IMediator mediator,
        ILoggerFactory loggerFactory)
    {
        var logger = loggerFactory.CreateLogger("Commerce.Mcp.OAuth");
        try
        {
            var result = await mediator.Send(new McpOAuthRegisterCommand(
                body.RedirectUris,
                body.GrantTypes,
                body.ResponseTypes,
                body.Scope,
                body.ClientName));
            logger.LogInformation(
                "OAuth DCR registered client_id={ClientId} name={ClientName} redirects={Redirects}",
                result.ClientId, result.ClientName, string.Join(", ", result.RedirectUris));
            var response = new Dictionary<string, object>
            {
                ["client_id"] = result.ClientId,
                ["redirect_uris"] = result.RedirectUris,
                ["grant_types"] = result.GrantTypes,
                ["response_types"] = result.ResponseTypes
            };
            if (!string.IsNullOrEmpty(result.ClientName))
                response["client_name"] = result.ClientName!;
            return Results.Json(response, JsonOptions);
        }
        catch (McpOAuthException ex)
        {
            logger.LogWarning(
                "OAuth DCR rejected: {Error} — {Description} (client_name={ClientName}, redirects={Redirects})",
                ex.ErrorCode,
                ex.ErrorDescription,
                body.ClientName,
                body.RedirectUris is null ? "" : string.Join(", ", body.RedirectUris));
            return Results.BadRequest(new { error = ex.ErrorCode, error_description = ex.ErrorDescription });
        }
    }

    private static string GetPublicBaseUrl(HttpContext context, McpOptions options)
    {
        var configured = options.PublicBaseUrl?.TrimEnd('/');
        if (!string.IsNullOrEmpty(configured) &&
            !configured.Contains("localhost", StringComparison.OrdinalIgnoreCase))
            return configured;

        // Prefer configured local URL; fall back to request host in Aspire.
        if (!string.IsNullOrEmpty(configured))
            return configured;

        var req = context.Request;
        return $"{req.Scheme}://{req.Host}";
    }

    private sealed record AuthorizeCompleteRequest(
        string ClientId,
        string RedirectUri,
        string? State,
        string? CodeChallenge,
        string? CodeChallengeMethod,
        string? Scope,
        string? Resource,
        string? ClientName = null);

    private sealed record DcrRequest(
        [property: JsonPropertyName("redirect_uris")] string[] RedirectUris,
        [property: JsonPropertyName("grant_types")] string[]? GrantTypes,
        [property: JsonPropertyName("response_types")] string[]? ResponseTypes,
        [property: JsonPropertyName("scope")] string? Scope,
        [property: JsonPropertyName("client_name")] string? ClientName);
}
