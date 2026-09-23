using Azure.Search.Documents.Indexes;
using Azure.Search.Documents.Indexes.Models;

namespace Commerce.Infrastructure.Vectors.AzureSearch;

public static class AzureSearchProductsSchemas
{
    public const string IndexName = "products";
    public const string TextVectorizer = "text-vectorizer";
    public const string TextProfile = "text-profile";
    public const string HnswAlgorithm = "hnsw-text";
    public const string SemanticConfig = "products-semantic";
    public const int VectorDimensions = 1536;

    public static SearchIndex CreateProductsIndex(
        string openAiEndpoint,
        string openAiApiKey,
        string embeddingDeployment)
    {
        var vectorizer = new AzureOpenAIVectorizer(TextVectorizer)
        {
            Parameters = new AzureOpenAIVectorizerParameters
            {
                ResourceUri = new Uri(openAiEndpoint),
                ApiKey = openAiApiKey,
                DeploymentName = embeddingDeployment,
                ModelName = embeddingDeployment.Contains("large", StringComparison.OrdinalIgnoreCase)
                    ? AzureOpenAIModelName.TextEmbedding3Large
                    : AzureOpenAIModelName.TextEmbedding3Small
            }
        };

        var algorithm = new HnswAlgorithmConfiguration(HnswAlgorithm);
        var profile = new VectorSearchProfile(TextProfile, HnswAlgorithm)
        {
            VectorizerName = TextVectorizer
        };

        var vectorSearch = new VectorSearch();
        vectorSearch.Algorithms.Add(algorithm);
        vectorSearch.Profiles.Add(profile);
        vectorSearch.Vectorizers.Add(vectorizer);

        var semantic = new SemanticSearch
        {
            Configurations =
            {
                new SemanticConfiguration(
                    SemanticConfig,
                    new SemanticPrioritizedFields
                    {
                        TitleField = new SemanticField("title"),
                        ContentFields = { new SemanticField("description") },
                        KeywordsFields =
                        {
                            new SemanticField("category"),
                            new SemanticField("brand"),
                            new SemanticField("seller")
                        }
                    })
            }
        };

        return new SearchIndex(IndexName)
        {
            Fields =
            {
                new SimpleField("id", SearchFieldDataType.String) { IsKey = true, IsFilterable = true },
                new SearchableField("tenant_id") { IsFilterable = true, IsFacetable = true },
                new SearchableField("sku") { IsFilterable = true },
                new SimpleField("product_id", SearchFieldDataType.String) { IsFilterable = true },
                new SimpleField("sku_id", SearchFieldDataType.String) { IsFilterable = true },
                new SimpleField("seller_id", SearchFieldDataType.String) { IsFilterable = true },
                new SearchableField("title") { AnalyzerName = LexicalAnalyzerName.EsMicrosoft },
                new SearchableField("description") { AnalyzerName = LexicalAnalyzerName.EsMicrosoft },
                new SearchableField("category") { IsFilterable = true, IsFacetable = true },
                new SearchableField("brand") { IsFilterable = true, IsFacetable = true },
                new SearchableField("seller") { IsFilterable = true, IsFacetable = true },
                new SimpleField("price", SearchFieldDataType.Double) { IsFilterable = true, IsSortable = true },
                new SimpleField("stock", SearchFieldDataType.Int32) { IsFilterable = true, IsSortable = true },
                new SimpleField("is_active", SearchFieldDataType.Boolean) { IsFilterable = true },
                new SimpleField("image_url", SearchFieldDataType.String),
                new SimpleField("image_status", SearchFieldDataType.String) { IsFilterable = true, IsFacetable = true },
                new SimpleField("doc_type", SearchFieldDataType.String) { IsFilterable = true, IsFacetable = true },
                new SimpleField("option_keys", SearchFieldDataType.Collection(SearchFieldDataType.String))
                {
                    IsFilterable = true,
                    IsFacetable = true
                },
                new SimpleField("option_pairs", SearchFieldDataType.Collection(SearchFieldDataType.String))
                {
                    IsFilterable = true,
                    IsFacetable = true
                },
                new SearchField("title_vector", SearchFieldDataType.Collection(SearchFieldDataType.Single))
                {
                    IsSearchable = true,
                    VectorSearchDimensions = VectorDimensions,
                    VectorSearchProfileName = TextProfile
                },
                new SearchField("description_vector", SearchFieldDataType.Collection(SearchFieldDataType.Single))
                {
                    IsSearchable = true,
                    VectorSearchDimensions = VectorDimensions,
                    VectorSearchProfileName = TextProfile
                },
                new SearchField("image_vector", SearchFieldDataType.Collection(SearchFieldDataType.Single))
                {
                    IsSearchable = true,
                    VectorSearchDimensions = VectorDimensions,
                    VectorSearchProfileName = TextProfile
                }
            },
            VectorSearch = vectorSearch,
            SemanticSearch = semantic
        };
    }
}
