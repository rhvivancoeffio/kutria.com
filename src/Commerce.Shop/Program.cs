var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.MapGet("/", () => Results.Ok(new
{
    service = "Commerce.Shop",
    status = "ok"
}));

app.MapGet("/health", () => Results.Ok(new { status = "healthy" }));

app.Run();
