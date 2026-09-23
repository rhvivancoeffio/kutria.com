using Commerce.Application;
using Commerce.Application.Configuration;
using Commerce.Domain.Tenants;
using Commerce.Infrastructure;
using Commerce.Infrastructure.Persistence;
using Commerce.Infrastructure.Auth;
using Commerce.Mcp.Mcp;
using Commerce.Mcp.Middleware;
using Finbuckle.MultiTenant.AspNetCore.Extensions;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using ModelContextProtocol.AspNetCore.Authentication;
using ModelContextProtocol.Protocol;
using Commerce.Application.Abstracts;

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddEnvironmentVariables();

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration, configureAspNetStrategies: false);
builder.Services.AddScoped<McpToolsHandler>();
builder.Services.AddHttpContextAccessor();

var mcpSection = builder.Configuration.GetSection(McpOptions.SectionName);
builder.Services.Configure<McpOptions>(mcpSection);
var mcpOptions = mcpSection.Get<McpOptions>() ?? new McpOptions();
var publicBase = (mcpOptions.PublicBaseUrl ?? "https://mcp.kutria.com").TrimEnd('/');
var frontendBase = (mcpOptions.FrontendBaseUrl ?? "https://kutria.com").TrimEnd('/');

var signingKey = builder.Configuration["Auth:SigningKey"];
if (string.IsNullOrWhiteSpace(signingKey))
    signingKey = "kutria-dev-signing-key";

builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = McpAuthenticationDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuers = [publicBase, publicBase + "/"],
            ValidAudiences = [publicBase, publicBase + "/"],
            IssuerSigningKey = AuthSigningKeys.CreateSymmetricKey(signingKey)
        };
        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = async ctx =>
            {
                var tenantId = ctx.Principal?.FindFirst("tenant_id")?.Value;
                if (string.IsNullOrEmpty(tenantId))
                    return;

                var store = ctx.HttpContext.RequestServices.GetRequiredService<ITenantStore>();
                var tenant = await store.GetByIdAsync(tenantId)
                    ?? await store.GetByIdentifierAsync(tenantId);
                if (tenant is not null)
                    TenantBootstrap.SetCurrentTenant(ctx.HttpContext.RequestServices, tenant);
            }
        };
    })
    .AddMcp(options =>
    {
        options.ResourceMetadata = new ModelContextProtocol.Authentication.ProtectedResourceMetadata
        {
            Resource = publicBase,
            AuthorizationServers = { publicBase },
            ScopesSupported = ["mcp:read", "mcp:write"],
            ResourceDocumentation = $"{frontendBase}/docs"
        };
    });

builder.Services.AddAuthorization();
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.AllowAnyHeader().AllowAnyMethod().AllowAnyOrigin());
});

builder.Services.AddMcpServer(options =>
    {
        options.ServerInfo = new Implementation { Name = "Kutria Commerce MCP", Version = "1.0.0" };
        options.Capabilities = new ServerCapabilities { Tools = new ToolsCapability() };
    })
    .WithHttpTransport(transport =>
    {
        transport.ConfigureSessionOptions = (httpContext, mcpServerOptions, _) =>
        {
            var handler = httpContext.RequestServices.GetRequiredService<McpToolsHandler>();
            mcpServerOptions.Handlers.ListToolsHandler = (req, ct) => handler.HandleListToolsAsync(req, ct);
            mcpServerOptions.Handlers.CallToolHandler = (req, ct) => handler.HandleCallToolAsync(req, ct);
            return Task.CompletedTask;
        };
    });

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors();
app.UseMultiTenant();
app.UseMiddleware<ApiKeyAuthenticationMiddleware>();
app.UseAuthentication();
app.UseAuthorization();

app.MapMcpOAuthEndpoints();

if (mcpOptions.Enabled)
{
    app.MapMcp().RequireAuthorization();
}

// MapMcp owns GET / (streamable HTTP). Do not map another GET / here.
app.MapGet("/health", () => Results.Ok(new { status = "ok", service = "commerce-mcp" }));
app.MapGet("/info", () => Results.Ok(new
{
    name = "kutria-mcp",
    resource = publicBase,
    docs = $"{frontendBase}/docs"
}));

app.Run();

public partial class Program;
