var builder = DistributedApplication.CreateBuilder(args);

var dbProvider = Environment.GetEnvironmentVariable("DB_PROVIDER")
    ?? Environment.GetEnvironmentVariable("Database__Provider")
    ?? "PostgreSQL";
var useSqlServer = string.Equals(dbProvider, "SqlServer", StringComparison.OrdinalIgnoreCase);

var vectorProvider = Environment.GetEnvironmentVariable("VECTOR_PROVIDER")
    ?? Environment.GetEnvironmentVariable("Vector__Provider")
    ?? builder.Configuration["Vector:Provider"]
    ?? "Qdrant";
var useAzureSearch = string.Equals(vectorProvider, "AzureSearch", StringComparison.OrdinalIgnoreCase);
var azureSearchEndpoint = Environment.GetEnvironmentVariable("AzureSearch__Endpoint")
    ?? builder.Configuration["AzureSearch:Endpoint"]
    ?? Environment.GetEnvironmentVariable("AZURE_SEARCH_ENDPOINT");
var azureSearchApiKey = Environment.GetEnvironmentVariable("AzureSearch__ApiKey")
    ?? builder.Configuration["AzureSearch:ApiKey"]
    ?? Environment.GetEnvironmentVariable("AZURE_SEARCH_API_KEY");

IResourceBuilder<IResourceWithConnectionString> db;
string databaseProviderName;
if (useSqlServer)
{
    // Same as channel/builder: let Aspire generate SA password (custom weak passwords cause container exit).
    var sql = builder.AddSqlServer("commerce-sql").PublishAsConnectionString();
    databaseProviderName = "SqlServer";
    db = sql.AddDatabase("Commerce");
}
else
{
    var postgresPassword = builder.AddParameter("postgres-password", "commerce");
    var postgres = builder.AddPostgres("commerce-postgres")
        .WithPassword(postgresPassword)
        .WithPgAdmin();
    databaseProviderName = "PostgreSQL";
    db = postgres.AddDatabase("Commerce");
}

#pragma warning disable ASPIRECERTIFICATES001
var redis = builder.AddRedis("commerce-redis")
    .WithoutHttpsCertificate()
    .PublishAsConnectionString();
#pragma warning restore ASPIRECERTIFICATES001

var rabbit = builder.AddRabbitMQ("commerce-rabbitmq")
    .WithEndpoint(port: 5693, targetPort: 5672, name: "amqp")
    .WithManagementPlugin(port: 15683);

var qdrant = builder.AddContainer("commerce-qdrant", "qdrant/qdrant", "v1.13.4")
    .WithHttpEndpoint(port: 6333, targetPort: 6333, name: "http")
    .WithEndpoint(port: 6334, targetPort: 6334, name: "grpc");

var brainRaw = Path.GetFullPath(Path.Combine(builder.AppHostDirectory, "..", "..", "data", "brains-raw"));

var azurite = builder.AddContainer("commerce-azurite", "mcr.microsoft.com/azure-storage/azurite", "3.36.0")
    .WithArgs(
        "azurite",
        "-l", "/data",
        "--blobHost", "0.0.0.0",
        "--queueHost", "0.0.0.0",
        "--tableHost", "0.0.0.0",
        "--skipApiVersionCheck")
    .WithEndpoint(port: 10000, targetPort: 10000, name: "blob")
    .WithEndpoint(port: 10001, targetPort: 10001, name: "queue")
    .WithEndpoint(port: 10002, targetPort: 10002, name: "table");
_ = azurite;

// Well-known Azurite account; Blob/Table endpoints use published host ports.
const string azureTablesConnection =
    "DefaultEndpointsProtocol=http;AccountName=devstoreaccount1;AccountKey=Eby8vdM02xNOcqFlqUwJPLlmEtlCDXJ1OUzFT50uSRZ6IFsuFq2UVErCz4I6tq/K1SZFPTOtr/KBHBeksoGMGw==;BlobEndpoint=http://127.0.0.1:10000/devstoreaccount1;QueueEndpoint=http://127.0.0.1:10001/devstoreaccount1;TableEndpoint=http://127.0.0.1:10002/devstoreaccount1;";

var api = builder.AddProject<Projects.Commerce_Api>("commerce-api")
    .WithHttpEndpoint(port: 5050, name: "api-http")
    .WithReference(db, connectionName: "Commerce")
    .WithReference(redis, connectionName: "Redis")
    .WithReference(rabbit, connectionName: "RabbitMQ")
    .WaitFor(db)
    .WaitFor(redis)
    .WithEnvironment("ASPNETCORE_ENVIRONMENT", "Development")
    .WithEnvironment("Database__Provider", databaseProviderName)
    .WithEnvironment("ConnectionStrings__AzureTables", azureTablesConnection)
    .WithEnvironment("Queue__Provider", "RabbitMQ")
    .WithEnvironment("Vector__Provider", useAzureSearch ? "AzureSearch" : "Qdrant")
    .WithEnvironment("DataIngestion__DispatchIntervalMinutes", "15")
    .WithEnvironment("Brain__RawPath", brainRaw)
    .WithHttpHealthCheck("/health");

var mcp = builder.AddProject<Projects.Commerce_Mcp>("commerce-mcp")
    .WithHttpEndpoint(port: 5055, name: "mcp-http")
    .WithReference(db, connectionName: "Commerce")
    .WithReference(redis, connectionName: "Redis")
    .WaitFor(db)
    .WaitFor(redis)
    .WaitFor(api)
    .WithEnvironment("ASPNETCORE_ENVIRONMENT", "Development")
    .WithEnvironment("Database__Provider", databaseProviderName)
    .WithEnvironment("Queue__Provider", "RabbitMQ")
    .WithEnvironment("Vector__Provider", useAzureSearch ? "AzureSearch" : "Qdrant")
    .WithEnvironment("Mcp__PublicBaseUrl", "http://localhost:5055")
    .WithEnvironment("Mcp__FrontendBaseUrl", "http://localhost:3000")
    .WithHttpHealthCheck("/health");

var worker = builder.AddProject<Projects.Commerce_Worker>("commerce-worker")
    .WithReference(db, connectionName: "Commerce")
    .WithReference(redis, connectionName: "Redis")
    .WithReference(rabbit, connectionName: "RabbitMQ")
    .WaitFor(db)
    .WaitFor(redis)
    .WaitFor(api)
    .WithEnvironment("DOTNET_ENVIRONMENT", "Development")
    .WithEnvironment("Database__Provider", databaseProviderName)
    .WithEnvironment("ConnectionStrings__AzureTables", azureTablesConnection)
    .WithEnvironment("Queue__Provider", "RabbitMQ")
    .WithEnvironment("Vector__Provider", useAzureSearch ? "AzureSearch" : "Qdrant")
    .WithEnvironment("DataIngestion__DispatchIntervalMinutes", "15")
    .WithEnvironment("Brain__RawPath", brainRaw);

if (useAzureSearch)
{
    if (string.IsNullOrWhiteSpace(azureSearchEndpoint) || string.IsNullOrWhiteSpace(azureSearchApiKey))
    {
        throw new InvalidOperationException(
            "Vector__Provider=AzureSearch requires AzureSearch__Endpoint and AzureSearch__ApiKey (env or AppHost/API user-secrets).");
    }

    api = api
        .WithEnvironment("AzureSearch__Endpoint", azureSearchEndpoint)
        .WithEnvironment("AzureSearch__ApiKey", azureSearchApiKey)
        .WithEnvironment("ConnectionStrings__AzureSearch", azureSearchEndpoint);
    mcp = mcp
        .WithEnvironment("AzureSearch__Endpoint", azureSearchEndpoint)
        .WithEnvironment("AzureSearch__ApiKey", azureSearchApiKey)
        .WithEnvironment("ConnectionStrings__AzureSearch", azureSearchEndpoint);
    worker = worker
        .WithEnvironment("AzureSearch__Endpoint", azureSearchEndpoint)
        .WithEnvironment("AzureSearch__ApiKey", azureSearchApiKey)
        .WithEnvironment("ConnectionStrings__AzureSearch", azureSearchEndpoint);
}
else
{
    api = api.WithEnvironment("ConnectionStrings__Qdrant", qdrant.GetEndpoint("grpc"));
    mcp = mcp.WithEnvironment("ConnectionStrings__Qdrant", qdrant.GetEndpoint("grpc"));
    worker = worker.WithEnvironment("ConnectionStrings__Qdrant", qdrant.GetEndpoint("grpc"));
}

// Frontend is not hosted in Aspire (same as template/builder/channel): use `make run-frontend`.
// RabbitMQ and Qdrant are not WaitFor dependencies so a missing broker does not block API startup.
// MCP host: http://localhost:5055 (prod: https://mcp.kutria.com)
builder.Build().Run();
