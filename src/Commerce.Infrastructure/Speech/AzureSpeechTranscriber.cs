using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Commerce.Application.Abstracts;

namespace Commerce.Infrastructure.Speech;

public sealed class AzureSpeechTranscriber(IConfiguration configuration, IHttpClientFactory http) : ISpeechTranscriber
{
    private const int MaxBytes = 10 * 1024 * 1024;

    public async Task<string> TranscribeAsync(Stream audio, string? contentType, CancellationToken cancellationToken = default)
    {
        var key = configuration["Speech:Key"];
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new InvalidOperationException("La transcripción no está activa.");
        }

        using var buffer = new MemoryStream();
        await audio.CopyToAsync(buffer, cancellationToken);
        if (buffer.Length == 0)
        {
            throw new InvalidOperationException("El audio es requerido.");
        }

        if (buffer.Length > MaxBytes)
        {
            throw new InvalidOperationException("El audio debe pesar máximo 10MB.");
        }

        var mime = Mime(contentType);
        var locale = Locale(configuration["Speech:Language"] ?? "es");
        var endpoint = Endpoint();
        var version = configuration["Speech:ApiVersion"] ?? "2025-10-15";
        var url = $"{endpoint}/speechtotext/transcriptions:transcribe?api-version={Uri.EscapeDataString(version)}";

        using var form = new MultipartFormDataContent();
        var file = new ByteArrayContent(buffer.ToArray());
        file.Headers.ContentType = MediaTypeHeaderValue.Parse(mime);
        form.Add(file, "audio", $"audio.{Extension(mime)}");
        form.Add(new StringContent(JsonSerializer.Serialize(new { locales = new[] { locale } }), Encoding.UTF8), "definition");

        var client = http.CreateClient("AzureSpeech");
        using var request = new HttpRequestMessage(HttpMethod.Post, url) { Content = form };
        request.Headers.Add("Ocp-Apim-Subscription-Key", key);
        using var response = await client.SendAsync(request, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException("No pudimos transcribir el audio. Revisa que la transcripción esté activa o escribe el mensaje.");
        }

        return ReadText(body)
            ?? throw new InvalidOperationException("No pudimos transcribir el audio. Revisa que la transcripción esté activa o escribe el mensaje.");
    }

    private string Endpoint()
    {
        var configured = (configuration["Speech:Endpoint"] ?? string.Empty).Trim().TrimEnd('/');
        if (!string.IsNullOrWhiteSpace(configured))
        {
            return configured;
        }

        var region = configuration["Speech:Region"] ?? "eastus";
        return $"https://{region}.api.cognitive.microsoft.com";
    }

    private static string? ReadText(string body)
    {
        using var document = JsonDocument.Parse(body);
        var combined = Texts(document.RootElement, "combinedPhrases");
        if (!string.IsNullOrWhiteSpace(combined))
        {
            return combined;
        }

        var phrases = Texts(document.RootElement, "phrases");
        return string.IsNullOrWhiteSpace(phrases) ? null : phrases;
    }

    private static string Texts(JsonElement root, string property)
    {
        if (!root.TryGetProperty(property, out var items) || items.ValueKind != JsonValueKind.Array)
        {
            return string.Empty;
        }

        return string.Join(' ', items.EnumerateArray()
            .Select(item => item.TryGetProperty("text", out var text) ? text.GetString() : null)
            .Where(text => !string.IsNullOrWhiteSpace(text))
            .Select(text => text!.Trim()));
    }

    private static string Mime(string? contentType)
    {
        var mime = (contentType ?? "audio/webm").Split(';')[0].Trim().ToLowerInvariant();
        if (mime is "audio/webm" or "audio/ogg" or "audio/mpeg" or "audio/mp4" or "audio/m4a" or "audio/x-m4a" or "audio/wav" or "audio/x-wav" or "audio/aac")
        {
            return mime;
        }

        throw new InvalidOperationException("El formato de audio no es compatible.");
    }

    private static string Extension(string mime)
        => mime switch
        {
            "audio/mp4" or "audio/m4a" or "audio/x-m4a" => "m4a",
            "audio/mpeg" => "mp3",
            "audio/wav" or "audio/x-wav" => "wav",
            "audio/ogg" => "ogg",
            "audio/aac" => "aac",
            _ => "webm"
        };

    private static string Locale(string language)
    {
        var raw = language.Trim();
        if (string.IsNullOrWhiteSpace(raw))
        {
            return "es-ES";
        }

        if (raw.Contains('-'))
        {
            return raw;
        }

        return raw.ToLowerInvariant() switch
        {
            "es" => "es-ES",
            "en" => "en-US",
            "pt" => "pt-BR",
            "fr" => "fr-FR",
            _ => $"{raw}-{raw.ToUpperInvariant()}"
        };
    }
}
