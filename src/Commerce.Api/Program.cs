using System.Text.Json;
using System.Text.Json.Serialization;
using Carter;
using Finbuckle.MultiTenant.AspNetCore.Extensions;
using Commerce.Api.Middleware;
using Commerce.Application;
using Commerce.Infrastructure;
using Commerce.Infrastructure.Persistence;
using Commerce.Infrastructure.Vectors;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddEnvironmentVariables();

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
    options.SerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter(System.Text.Json.JsonNamingPolicy.CamelCase));
});

builder.Services.AddApplication();
builder.Services.AddHttpContextAccessor();
builder.Services.AddInfrastructure(builder.Configuration, configureAspNetStrategies: true);
builder.Services.AddCarter();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddCors(options =>
{
    var chatOrigins = builder.Configuration.GetSection("Chat:AllowedOrigins").Get<string[]>() ?? [];
    options.AddDefaultPolicy(policy =>
        policy.AllowAnyHeader().AllowAnyMethod().AllowAnyOrigin());
    options.AddPolicy("Chat", policy =>
    {
        policy.AllowAnyHeader().AllowAnyMethod();
        if (chatOrigins.Length == 0)
        {
            policy.AllowAnyOrigin();
            return;
        }

        policy.WithOrigins(chatOrigins);
    });
});

var app = builder.Build();

await app.Services.MigrateAndSeedAsync();
await app.Services.EnsureVectorIndexesAsync();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors();
app.UseMultiTenant();
app.UseMiddleware<RequireResolvedTenantMiddleware>();
app.MapCarter();
app.MapGet("/health", () => Results.Ok(new { status = "ok" }));
app.MapGet("/", () => app.Environment.IsDevelopment()
    ? Results.Redirect("/swagger")
    : Results.Ok(new { name = "kutria", status = "ok" }));

app.Run();

public partial class Program;
