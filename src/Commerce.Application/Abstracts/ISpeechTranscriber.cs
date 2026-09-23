namespace Commerce.Application.Abstracts;

public interface ISpeechTranscriber
{
    Task<string> TranscribeAsync(Stream audio, string? contentType, CancellationToken cancellationToken = default);
}
