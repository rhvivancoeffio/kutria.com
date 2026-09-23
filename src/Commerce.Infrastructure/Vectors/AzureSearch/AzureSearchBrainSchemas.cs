using Azure.Search.Documents.Indexes;
using Azure.Search.Documents.Indexes.Models;

namespace Commerce.Infrastructure.Vectors.AzureSearch;

/// <summary>
/// Shared Azure AI Search schema for brain collections (e.g. policy-brain).
/// Index name = collection name (lowercase / dashes).
/// </summary>
public static class AzureSearchBrainSchemas
{
    public const string ContentVector = "content_vector";
    public const string TextVectorizer = "brain-text-vectorizer";
    public const string TextProfile = "brain-text-profile";
    public const string HnswAlgorithm = "brain-hnsw";
    public const int VectorDimensions = 1536;

    /// <summary>Well-known brain indexes to create at startup (low-cost Free SKU slot).</summary>
    public static readonly string[] StartupIndexNames = ["policy-brain"];

    public static SearchIndex CreateBrainIndex(
        string indexName,
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

        return new SearchIndex(indexName)
        {
            Fields =
            {
                new SimpleField("id", SearchFieldDataType.String) { IsKey = true, IsFilterable = true },
                new SearchableField("tenant_id") { IsFilterable = true, IsFacetable = true },
                new SearchableField("workspace_id") { IsFilterable = true, IsFacetable = true },
                new SearchableField("point_id") { IsFilterable = true },
                new SearchableField("title") { AnalyzerName = LexicalAnalyzerName.EsMicrosoft },
                new SearchableField("content") { AnalyzerName = LexicalAnalyzerName.EsMicrosoft },
                new SearchableField("type") { IsFilterable = true, IsFacetable = true },
                new SearchableField("language") { IsFilterable = true, IsFacetable = true },
                new SearchableField("job_id") { IsFilterable = true },
                new SearchableField("policy_id") { IsFilterable = true },
                new SearchableField("is_evaluated") { IsFilterable = true, IsFacetable = true },
                new SearchableField("applicable_sellers") { IsFilterable = true },
                new SearchableField("effective_from") { IsFilterable = true },
                new SearchableField("effective_to") { IsFilterable = true },
                new SimpleField("source_url", SearchFieldDataType.String),
                new SimpleField("chunk_id", SearchFieldDataType.String) { IsFilterable = true },
                new SimpleField("point_key", SearchFieldDataType.String) { IsFilterable = true },
                new SearchField(ContentVector, SearchFieldDataType.Collection(SearchFieldDataType.Single))
                {
                    IsSearchable = true,
                    VectorSearchDimensions = VectorDimensions,
                    VectorSearchProfileName = TextProfile
                }
            },
            VectorSearch = vectorSearch
        };
    }
}
