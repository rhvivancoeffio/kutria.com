using Commerce.Application;
using Commerce.Infrastructure;

var builder = Host.CreateApplicationBuilder(args);

builder.Configuration.AddEnvironmentVariables();

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration, hostWorkflows: true);
builder.Services.AddHostedService<Commerce.Worker.Workers.EventStreamGcWorker>();
builder.Services.AddHostedService<Commerce.Worker.Workers.BrainIngestWorker>();
builder.Services.AddHostedService<Commerce.Worker.Workers.PolicyEvalWorker>();
builder.Services.AddHostedService<Commerce.Worker.Workers.ChatEventsWorker>();
builder.Services.AddHostedService<Commerce.Worker.Workers.AgentGenerationAuditWorker>();
builder.Services.AddHostedService<Commerce.Worker.Workers.DataIngestWorker>();
builder.Services.AddHostedService<Commerce.Worker.Workers.CatalogVectorIngestWorker>();
builder.Services.AddHostedService<Commerce.Worker.Workers.CatalogImageVectorIngestWorker>();
builder.Services.AddHostedService<Commerce.Worker.Workers.DataIngestDispatcher>();

var host = builder.Build();

// Schema migrate/seed and vector index bootstrap run only in Commerce.Api.

await host.RunAsync();
