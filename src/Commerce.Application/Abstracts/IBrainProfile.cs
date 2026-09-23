namespace Commerce.Application.Abstracts;

public sealed record BrainPublishedDocument(
    string TenantId,
    Guid? WorkspaceId,
    Guid JobId,
    string FileName,
    string SourceUrl,
    string Text,
    string MetadataJson);

public interface IBrainProfile
{
    string Key { get; }

    string CollectionName { get; }

    IReadOnlyList<BrainPoint> BuildPoints(BrainPublishedDocument document);
}

public interface IBrainProfileRegistry
{
    IBrainProfile Get(string key);
}
