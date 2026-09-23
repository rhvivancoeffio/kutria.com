namespace Commerce.Application.Features.Policies.DownloadPolicyFile;

public sealed record DownloadPolicyFileResult(string FileName, string ContentType, byte[] Content);
