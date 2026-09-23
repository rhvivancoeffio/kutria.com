using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Commerce.Application.Abstracts;
using Commerce.Infrastructure.Agents;
using Commerce.Infrastructure.Agents.Definitions.Checkout;
using Commerce.Infrastructure.Agents.Definitions.DiscoveryAndCart;
using Commerce.Infrastructure.Agents.Engines;
using Commerce.Infrastructure.Agents.Definitions.Fulfillment;
using Commerce.Infrastructure.Agents.Definitions.OperationsAdvisor;
using Commerce.Infrastructure.Agents.Definitions.PaymentWebhook;
using Commerce.Infrastructure.Agents.Definitions.CorporateFaq;
using Commerce.Infrastructure.Agents.Definitions.PostSalesSupport;
using Commerce.Infrastructure.Agents.Definitions.BrandManager;
using Commerce.Infrastructure.Agents.Definitions.CategoryManager;
using Commerce.Infrastructure.Agents.Definitions.ProductManager;
using Commerce.Infrastructure.Agents.Definitions.ProductContentByImageGenerator;
using Commerce.Infrastructure.Agents.Definitions.ProductContentGenerator;
using Commerce.Infrastructure.Agents.Definitions.ProductImageGenerator;
using Commerce.Infrastructure.Agents.Definitions.ProductVideoGenerator;
using Commerce.Infrastructure.Agents.Definitions.ProductOnboardingCoordinator;
using Commerce.Infrastructure.Agents.Profile;
using Commerce.Infrastructure.Agents.History;
using Commerce.Infrastructure.Agents.Events;
using Commerce.Application.Features.CreateProduct;
using Commerce.Infrastructure.Agents.Tools;
using Commerce.Infrastructure.Messaging;
using Commerce.Infrastructure.Agents.Metadata;
using Commerce.Infrastructure.Auth;
using Commerce.Infrastructure.Mcp;
using Commerce.Application.Abstracts.Mcp;
using Commerce.Application.Configuration;
using Commerce.Infrastructure.Persistence;
using Commerce.Infrastructure.Vectors;
using Commerce.Infrastructure.Vectors.Common;
using Commerce.Infrastructure.Vectors.Qdrant;
using Commerce.Infrastructure.Vectors.AzureSearch;
using Commerce.Infrastructure.Speech;
using Commerce.Infrastructure.Yaml;
using Commerce.Infrastructure.Integrations;
using Commerce.Infrastructure.Tours;
using Commerce.Infrastructure.DataIngestion;
using Commerce.Infrastructure.EventStreams;
using Commerce.Infrastructure.Brains;
using Commerce.Infrastructure.Policies;
using Commerce.Application.Abstracts.Tours;
using Commerce.Infrastructure.Accounts;
using Microsoft.AspNetCore.Http;

namespace Commerce.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration,
        bool configureAspNetStrategies = false,
        bool hostWorkflows = false)
    {
        services.AddPersistence(configuration, configureAspNetStrategies);
        services.AddSingleton<IPasswordHasher, Pbkdf2PasswordHasher>();
        services.AddSingleton<IAccessTokenIssuer, HmacAccessTokenIssuer>();
        services.AddSingleton<IAccessTokenValidator, HmacAccessTokenValidator>();
        services.Configure<McpOptions>(configuration.GetSection(McpOptions.SectionName));
        // Optional IHttpContextAccessor: API/MCP register it; Worker does not.
        services.AddScoped<IWorkspaceContext>(sp =>
            new WorkspaceContext(sp.GetService<IHttpContextAccessor>()));
        services.AddScoped<IMcpOAuthStore, DbMcpOAuthStore>();
        services.AddScoped<IMcpTokenService, McpTokenService>();
        services.AddSingleton<IToolCatalog, ToolCatalogService>();
        services.AddScoped<IMcpToolInvoker, McpToolInvoker>();

        var redis = configuration.GetConnectionString("Redis");
        if (!string.IsNullOrWhiteSpace(redis))
        {
            services.AddStackExchangeRedisCache(options => options.Configuration = redis);
        }
        else
        {
            services.AddDistributedMemoryCache();
        }

        services.AddMemoryCache();
        services.AddSingleton<IYamlMetadataService, YamlMetadataService>();
        services.AddSingleton<IIntegrationsMetadataService, IntegrationsMetadataService>();
        services.AddSingleton<IToursMetadataService, ToursMetadataService>();

        services.Configure<MeetGravityOptions>(configuration.GetSection(MeetGravityOptions.SectionName));
        services.Configure<DataIngestionOptions>(configuration.GetSection(DataIngestionOptions.SectionName));
        services.Configure<CatalogImageOptions>(configuration.GetSection(CatalogImageOptions.SectionName));
        services.Configure<EventStreamsOptions>(configuration.GetSection(EventStreamsOptions.SectionName));
        services.AddHttpClient("MeetGravity", (sp, client) =>
        {
            var opts = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<MeetGravityOptions>>().Value;
            var baseUrl = (opts.BaseUrl ?? "").Trim().TrimEnd('/');
            if (!string.IsNullOrWhiteSpace(baseUrl))
                client.BaseAddress = new Uri(baseUrl + "/");
            client.Timeout = TimeSpan.FromSeconds(60);
        });
        services.AddScoped<IMeetGravityHeadlessClient, MeetGravityHeadlessClient>();
        services.AddScoped<IMeetGravityStoreProvisioner, MeetGravityStoreProvisioner>();
        services.AddHttpClient("GravityStoreData")
            .ConfigureHttpClient(client =>
            {
                client.Timeout = TimeSpan.FromSeconds(120);
            })
            .ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler
            {
                // Azure App Service often closes idle keep-alives; recycle before scavenge hits RST.
                PooledConnectionLifetime = TimeSpan.FromMinutes(2),
                PooledConnectionIdleTimeout = TimeSpan.FromMinutes(1),
                MaxConnectionsPerServer = 10
            });
        services.AddScoped<IGravityStoreDataClient, GravityStoreDataClient>();
        services.AddSingleton<IDataIngestPayloadBlobStore>(sp =>
        {
            var config = sp.GetRequiredService<IConfiguration>();
            var log = sp.GetRequiredService<ILoggerFactory>().CreateLogger("DataIngestPayloadBlobStore");
            var connection = ResolveAzureBlobConnection(config);
            if (string.IsNullOrWhiteSpace(connection))
            {
                log.LogWarning(
                    "ConnectionStrings:AzureBlobs / AzureStorage / AzureTables (with BlobEndpoint) is not set. Using InMemoryDataIngestPayloadBlobStore.");
                return new InMemoryDataIngestPayloadBlobStore();
            }

            log.LogInformation(
                "Using AzureDataIngestPayloadBlobStore. Container={Container} Connection preview={Preview}",
                AzureDataIngestPayloadBlobStore.ContainerName,
                connection.Length > 80 ? connection[..80] + "…" : connection);
            return new AzureDataIngestPayloadBlobStore(
                connection,
                sp.GetRequiredService<ILogger<AzureDataIngestPayloadBlobStore>>());
        });
        services.AddSingleton<IDataIngestTableStore>(sp =>
        {
            var config = sp.GetRequiredService<IConfiguration>();
            var log = sp.GetRequiredService<ILoggerFactory>().CreateLogger("DataIngestTableStore");
            var connection = config.GetConnectionString("AzureTables");
            if (string.IsNullOrWhiteSpace(connection))
            {
                log.LogWarning(
                    "COMMERCE_AZURE_TABLES / ConnectionStrings:AzureTables is not set. Using InMemoryDataIngestTableStore (data lost on restart).");
                return new InMemoryDataIngestTableStore();
            }

            log.LogInformation(
                "Using AzureDataIngestTableStore. Connection preview={Preview}",
                connection.Length > 80 ? connection[..80] + "…" : connection);
            return new AzureDataIngestTableStore(
                connection,
                sp.GetRequiredService<IDataIngestPayloadBlobStore>(),
                sp.GetRequiredService<ILogger<AzureDataIngestTableStore>>());
        });

        services.AddSingleton<IChatEventTableStore>(sp =>
        {
            var config = sp.GetRequiredService<IConfiguration>();
            var log = sp.GetRequiredService<ILoggerFactory>().CreateLogger("ChatEventTableStore");
            var connection = config.GetConnectionString("AzureTables");
            if (string.IsNullOrWhiteSpace(connection))
            {
                log.LogWarning(
                    "COMMERCE_AZURE_TABLES / ConnectionStrings:AzureTables is not set. Using InMemoryChatEventTableStore (data lost on restart).");
                return new InMemoryChatEventTableStore();
            }

            log.LogInformation(
                "Using AzureChatEventTableStore. Connection preview={Preview}",
                connection.Length > 80 ? connection[..80] + "…" : connection);
            return new AzureChatEventTableStore(
                connection,
                sp.GetRequiredService<ILogger<AzureChatEventTableStore>>());
        });

        services.AddSingleton<IEventStreamStore>(sp =>
        {
            var config = sp.GetRequiredService<IConfiguration>();
            var log = sp.GetRequiredService<ILoggerFactory>().CreateLogger("EventStreamStore");
            var connection = config.GetConnectionString("AzureTables");
            if (string.IsNullOrWhiteSpace(connection))
            {
                log.LogWarning(
                    "ConnectionStrings:AzureTables is not set. Using InMemoryEventStreamStore (data lost on restart).");
                return new InMemoryEventStreamStore(
                    sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<EventStreamsOptions>>());
            }

            log.LogInformation(
                "Using AzureEventStreamStore ({Table}). Connection preview={Preview}",
                AzureEventStreamStore.TableName,
                connection.Length > 80 ? connection[..80] + "…" : connection);
            return new AzureEventStreamStore(
                connection,
                sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<EventStreamsOptions>>(),
                sp.GetRequiredService<ILogger<AzureEventStreamStore>>());
        });

        services.AddSingleton<IAgentGenerationAuditPublisher, QueueAgentGenerationAuditPublisher>();
        services.AddSingleton<IAgentGenerationAuditStore>(sp =>
        {
            var config = sp.GetRequiredService<IConfiguration>();
            var log = sp.GetRequiredService<ILoggerFactory>().CreateLogger("AgentGenerationAuditStore");
            var connection = config.GetConnectionString("AzureTables");
            if (string.IsNullOrWhiteSpace(connection))
            {
                log.LogWarning(
                    "ConnectionStrings:AzureTables is not set. Using InMemoryAgentGenerationAuditStore.");
                return new InMemoryAgentGenerationAuditStore();
            }

            return new AzureAgentGenerationAuditStore(
                connection,
                sp.GetRequiredService<ILogger<AzureAgentGenerationAuditStore>>());
        });

        services.AddSingleton<IChatThreadStore>(sp =>
        {
            var config = sp.GetRequiredService<IConfiguration>();
            var log = sp.GetRequiredService<ILoggerFactory>().CreateLogger("ChatThreadStore");
            var provider = ResolveChatProvider(config);

            if (IsChatTableStorage(provider))
            {
                var tables = config.GetConnectionString("AzureTables");
                if (!string.IsNullOrWhiteSpace(tables))
                {
                    log.LogInformation("Chat provider=TableStorage history (commercechatthreads).");
                    return new AzureChatThreadStore(
                        tables,
                        config,
                        sp.GetRequiredService<ILogger<AzureChatThreadStore>>());
                }

                log.LogWarning(
                    "Chat:Provider=TableStorage but ConnectionStrings:AzureTables is missing. Falling back to Redis/in-memory.");
            }
            else if (!string.Equals(provider, "Redis", StringComparison.OrdinalIgnoreCase))
            {
                log.LogWarning(
                    "Unknown Chat:Provider={Provider}. Using Redis. Allowed: Redis, TableStorage.",
                    provider);
            }
            else
            {
                log.LogInformation("Chat provider=Redis history.");
            }

            return ActivatorUtilities.CreateInstance<RedisChatThreadStore>(sp);
        });
        services.AddSingleton<ISessionBuyerProfileStore>(sp =>
        {
            var config = sp.GetRequiredService<IConfiguration>();
            var log = sp.GetRequiredService<ILoggerFactory>().CreateLogger("SessionBuyerProfileStore");
            var provider = ResolveChatProvider(config);

            if (IsChatTableStorage(provider))
            {
                var tables = config.GetConnectionString("AzureTables");
                if (!string.IsNullOrWhiteSpace(tables))
                {
                    log.LogInformation("Chat provider=TableStorage buyer profile (commercebuyerprofiles).");
                    return new AzureSessionBuyerProfileStore(
                        tables,
                        sp.GetRequiredService<ILogger<AzureSessionBuyerProfileStore>>());
                }

                log.LogWarning(
                    "Chat:Provider=TableStorage but ConnectionStrings:AzureTables is missing. Falling back to Redis/in-memory for buyer profile.");
            }
            else if (!string.Equals(provider, "Redis", StringComparison.OrdinalIgnoreCase))
            {
                log.LogWarning(
                    "Unknown Chat:Provider={Provider}. Using Redis for buyer profile. Allowed: Redis, TableStorage.",
                    provider);
            }
            else
            {
                log.LogInformation("Chat provider=Redis buyer profile.");
            }

            return ActivatorUtilities.CreateInstance<RedisSessionBuyerProfileStore>(sp);
        });
        services.AddSingleton<ISessionBuyerProfileUpdater, SessionBuyerProfileUpdater>();
        services.AddSingleton<RedisChatHistoryProvider>();
        services.AddSingleton<ChatHistoryProvider>(sp => sp.GetRequiredService<RedisChatHistoryProvider>());
        services.AddSingleton<IChatEventPublisher, QueueChatEventPublisher>();
        services.AddSingleton<SessionBuyerProfileAIContextProvider>();
        services.AddSingleton<ChatEventsAIContextProvider>();
        services.AddScoped<IAgentDefinitionStore, AgentDefinitionStore>();
        services.AddScoped<IChatTurnEngine, BuyerSwitchChatEngine>();
        services.AddScoped<IChatTurnEngine, HandoffTriageChatEngine>();
        services.AddScoped<IAgentRuntime, AgentRuntime>();
        services.AddScoped<IProposalExecutor, RecordingProposalExecutor>();
        DiscoveryAndCartAgent.Add(services);
        CheckoutAgent.Add(services);
        PostSalesSupportAgent.Add(services);
        CorporateFaqAgent.Add(services);
        OperationsAdvisorAgent.Add(services);
        PaymentWebhookAgent.Add(services);
        FulfillmentAgent.Add(services);
        ProductOnboardingCoordinatorAgent.Add(services);
        BrandManagerAgent.Add(services);
        CategoryManagerAgent.Add(services);
        ProductManagerAgent.Add(services);
        ProductContentByImageGeneratorAgent.Add(services);
        ProductContentGeneratorAgent.Add(services);
        ProductImageGeneratorAgent.Add(services);
        ProductVideoGeneratorAgent.Add(services);
        services.AddScoped<IProductOnboardingCoordinator, ProductOnboardingCoordinatorService>();
        services.AddSingleton<IOnboardingWorkflowStore, RedisOnboardingWorkflowStore>();
        services.AddSingleton<IOnboardingImageStore, RedisOnboardingImageStore>();
        services.AddScoped<ICatalogTools, CatalogTools>();
        services.AddScoped<IOrderTools, OrderTools>();
        services.AddScoped<IKpiTools, KpiTools>();
        services.AddHttpClient("CatalogImageFetch", client =>
        {
            client.Timeout = TimeSpan.FromSeconds(30);
        });
        services.AddSingleton<Commerce.Application.Abstracts.IEmbeddingGenerator, PlaceholderEmbeddingGenerator>();

        services.AddHttpClient("AzureSpeech");
        services.AddSingleton<ISpeechTranscriber, AzureSpeechTranscriber>();
        services.AddSingleton<IMessageQueue>(_ => CreateQueue(configuration));
        RegisterVectorServices(services, configuration);
        services.AddSingleton<IBrainRawStore, LocalBrainRawStore>();
        services.AddSingleton<IBrainDocumentParser, LocalBrainDocumentParser>();
        services.AddScoped<IPolicyEvalCatalog, PolicyEvalCatalog>();
        services.AddSingleton<IPolicyEvalModel, PolicyEvalModel>();
        services.AddScoped<IPolicyEvalSearch, PolicyEvalSearch>();
        services.AddSingleton<IBrainProfile, PolicyBrainProfile>();
        services.AddSingleton<IBrainProfileRegistry, BrainProfileRegistry>();
        services.AddScoped<IBrainPipeline, BrainPipeline>();

        var endpoint = configuration["AzureOpenAI:Endpoint"];
        var apiKey = configuration["AzureOpenAI:ApiKey"];
        var deployment = configuration["AzureOpenAI:Deployment"] ?? "gpt-4o-mini";
        if (!string.IsNullOrWhiteSpace(endpoint) && !string.IsNullOrWhiteSpace(apiKey))
        {
            services.AddSingleton<IChatClient>(_ =>
                AzureOpenAiCompatibleClient.Create(endpoint, apiKey)
                    .GetChatClient(deployment)
                    .AsIChatClient());
        }

        if (hostWorkflows)
        {
            services.AddHostedService<WorkflowHostedService>();
        }

        return services;
    }

    private static void RegisterVectorServices(IServiceCollection services, IConfiguration configuration)
    {
        var provider = configuration["Vector:Provider"] ?? "Qdrant";
        var openAiEndpoint = configuration["AzureOpenAI:Endpoint"];
        var openAiKey = configuration["AzureOpenAI:ApiKey"];
        var embeddingDeployment = configuration["AzureOpenAI:EmbeddingDeployment"];
        var chatDeployment = configuration["AzureOpenAI:Deployment"] ?? "gpt-4o-mini";
        var enableSemantic = configuration.GetValue("Vector:EnableSemantic", false);

        var openAiReady = !string.IsNullOrWhiteSpace(openAiEndpoint)
            && !string.IsNullOrWhiteSpace(openAiKey)
            && !string.IsNullOrWhiteSpace(embeddingDeployment);

        if (openAiReady)
        {
            services.AddSingleton<ITextEmbeddingService>(_ =>
                new AzureOpenAiTextEmbeddingService(openAiEndpoint!, openAiKey!, embeddingDeployment!));
            services.AddSingleton<IImageVerbalizer>(sp =>
                new AzureOpenAiImageVerbalizer(
                    openAiEndpoint!,
                    openAiKey!,
                    chatDeployment,
                    sp.GetRequiredService<IHttpClientFactory>()));
            services.AddSingleton<ICatalogImageVectorService, CatalogImageVectorService>();
        }
        else
        {
            services.AddSingleton<IImageVerbalizer, NullImageVerbalizer>();
            services.AddSingleton<ICatalogImageVectorService, NullCatalogImageVectorService>();
        }

        if (string.Equals(provider, "AzureSearch", StringComparison.OrdinalIgnoreCase))
        {
            var searchEndpoint = configuration["AzureSearch:Endpoint"]
                ?? configuration.GetConnectionString("AzureSearch");
            var searchKey = configuration["AzureSearch:ApiKey"];
            var deleteIndexOnBootstrap = configuration.GetValue("AzureSearch:DeleteIndexOnBootstrap", false);

            if (openAiReady
                && !string.IsNullOrWhiteSpace(searchEndpoint)
                && !string.IsNullOrWhiteSpace(searchKey)
                && Uri.TryCreate(searchEndpoint, UriKind.Absolute, out var searchUri))
            {
                services.AddSingleton<ICatalogBrainIndex>(sp =>
                    new AzureSearchProductsIndex(
                        searchUri,
                        searchKey!,
                        sp.GetRequiredService<ITextEmbeddingService>(),
                        sp.GetRequiredService<ILogger<AzureSearchProductsIndex>>(),
                        enableSemantic));
                services.AddSingleton<ICatalogBrainWriter>(sp =>
                    (ICatalogBrainWriter)sp.GetRequiredService<ICatalogBrainIndex>());
                services.AddSingleton(sp =>
                    new AzureSearchBrainIndex(
                        searchUri,
                        searchKey!,
                        sp.GetRequiredService<ITextEmbeddingService>(),
                        sp.GetRequiredService<ILogger<AzureSearchBrainIndex>>(),
                        openAiEndpoint!,
                        openAiKey!,
                        embeddingDeployment!,
                        deleteIndexOnBootstrap));
                services.AddSingleton<IBrainIndex>(sp => sp.GetRequiredService<AzureSearchBrainIndex>());
                services.AddSingleton<IVectorIndexBootstrapper>(sp =>
                    new AzureSearchCompositeBootstrapper(
                        new AzureSearchProductsBootstrapper(
                            searchUri,
                            searchKey!,
                            openAiEndpoint!,
                            openAiKey!,
                            embeddingDeployment!,
                            sp.GetRequiredService<ILogger<AzureSearchProductsBootstrapper>>(),
                            deleteIndexOnBootstrap),
                        new AzureSearchBrainBootstrapper(sp.GetRequiredService<AzureSearchBrainIndex>())));
                services.AddSingleton<IVectorStore>(_ => new InMemoryVectorStore());
                return;
            }
        }

        // Default: Qdrant (or empty fallbacks)
        var qdrant = configuration.GetConnectionString("Qdrant");
        if (openAiReady
            && !string.IsNullOrWhiteSpace(qdrant)
            && TryParseQdrant(qdrant, out var host, out var port, out var https))
        {
            services.AddSingleton(sp =>
                new QdrantProductsIndex(
                    host,
                    port,
                    https,
                    sp.GetRequiredService<ITextEmbeddingService>(),
                    sp.GetRequiredService<ILogger<QdrantProductsIndex>>(),
                    configuration.GetValue("Qdrant:DeleteCollectionOnBootstrap", false)
                        || configuration.GetValue("AzureSearch:DeleteIndexOnBootstrap", false)));
            services.AddSingleton<ICatalogBrainIndex>(sp => sp.GetRequiredService<QdrantProductsIndex>());
            services.AddSingleton<ICatalogBrainWriter>(sp => sp.GetRequiredService<QdrantProductsIndex>());
            services.AddSingleton<IVectorIndexBootstrapper>(sp =>
                new QdrantProductsBootstrapper(sp.GetRequiredService<QdrantProductsIndex>()));
            services.AddSingleton<IVectorStore>(_ => new QdrantVectorStore(host, port, https));
            services.AddSingleton(sp =>
                CreateBrainIndex(configuration, sp.GetRequiredService<ILoggerFactory>()));
            return;
        }

        var empty = new EmptyCatalogBrainIndex();
        services.AddSingleton<ICatalogBrainIndex>(empty);
        services.AddSingleton<ICatalogBrainWriter>(empty);
        services.AddSingleton<IVectorIndexBootstrapper, NoOpVectorIndexBootstrapper>();
        services.AddSingleton<IVectorStore, InMemoryVectorStore>();
        services.AddSingleton(sp =>
            CreateBrainIndex(configuration, sp.GetRequiredService<ILoggerFactory>()));
    }

    private static IMessageQueue CreateQueue(IConfiguration configuration)
    {
        var provider = configuration["Queue:Provider"] ?? "RabbitMQ";
        var rabbit = configuration.GetConnectionString("RabbitMQ");
        var azure = configuration.GetConnectionString("AzureQueue");

        if (string.Equals(provider, "AzureQueue", StringComparison.OrdinalIgnoreCase)
            && !string.IsNullOrWhiteSpace(azure))
        {
            return new AzureStorageMessageQueue(azure);
        }

        if (string.Equals(provider, "RabbitMQ", StringComparison.OrdinalIgnoreCase)
            && !string.IsNullOrWhiteSpace(rabbit))
        {
            return new RabbitMqMessageQueue(rabbit);
        }

        return new InMemoryMessageQueue();
    }

    private static IBrainIndex CreateBrainIndex(IConfiguration configuration, ILoggerFactory loggerFactory)
    {
        var endpoint = configuration["AzureOpenAI:Endpoint"];
        var apiKey = configuration["AzureOpenAI:ApiKey"];
        var deployment = configuration["AzureOpenAI:EmbeddingDeployment"];
        var qdrant = configuration.GetConnectionString("Qdrant");
        if (string.IsNullOrWhiteSpace(endpoint)
            || string.IsNullOrWhiteSpace(apiKey)
            || string.IsNullOrWhiteSpace(deployment)
            || string.IsNullOrWhiteSpace(qdrant)
            || !TryParseQdrant(qdrant, out var host, out var port, out var https))
        {
            return new EmptyBrainIndex();
        }

        return new QdrantBrainIndex(
            host,
            port,
            https,
            endpoint,
            apiKey,
            deployment,
            loggerFactory.CreateLogger<QdrantBrainIndex>());
    }

    internal static bool TryParseQdrant(string endpoint, out string host, out int port, out bool https)
    {
        host = endpoint;
        port = 6334;
        https = false;
        if (Uri.TryCreate(endpoint, UriKind.Absolute, out var uri) && !string.IsNullOrWhiteSpace(uri.Host))
        {
            host = uri.Host;
            https = uri.Scheme == "https";
            port = uri.Port > 0 ? uri.Port : https ? 6334 : 6334;
            return true;
        }

        var parts = endpoint.Split(':', 2, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length == 0 || string.IsNullOrWhiteSpace(parts[0]))
        {
            return false;
        }

        host = parts[0];
        if (parts.Length == 2 && int.TryParse(parts[1], out var parsed))
        {
            port = parsed;
        }

        return true;
    }

    private static string ResolveChatProvider(IConfiguration config)
        => config["Chat:Provider"] ?? config["Chat:HistoryProvider"] ?? "Redis";

    private static bool IsChatTableStorage(string provider)
        => string.Equals(provider, "TableStorage", StringComparison.OrdinalIgnoreCase)
           || string.Equals(provider, "AzureTables", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Prefer AzureBlobs; else AzureStorage; else AzureTables when it already exposes BlobEndpoint (Azurite/full account).
    /// </summary>
    private static string? ResolveAzureBlobConnection(IConfiguration config)
    {
        var blobs = config.GetConnectionString("AzureBlobs");
        if (!string.IsNullOrWhiteSpace(blobs))
            return blobs;

        var storage = config.GetConnectionString("AzureStorage");
        if (!string.IsNullOrWhiteSpace(storage))
            return storage;

        var tables = config.GetConnectionString("AzureTables");
        if (string.IsNullOrWhiteSpace(tables))
            return null;

        if (tables.Contains("BlobEndpoint=", StringComparison.OrdinalIgnoreCase)
            || tables.Contains("UseDevelopmentStorage=true", StringComparison.OrdinalIgnoreCase)
            || (!tables.Contains("TableEndpoint=", StringComparison.OrdinalIgnoreCase)
                && tables.Contains("AccountName=", StringComparison.OrdinalIgnoreCase)))
        {
            return tables;
        }

        return null;
    }
}
