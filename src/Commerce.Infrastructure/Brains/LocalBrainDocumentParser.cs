using System.Text;
using System.Text.Json;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using UglyToad.PdfPig;
using Commerce.Application.Abstracts;

namespace Commerce.Infrastructure.Brains;

public sealed class LocalBrainDocumentParser : IBrainDocumentParser
{
    public Task<BrainParseResult> ParseAsync(
        string fileName,
        ReadOnlyMemory<byte> content,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        return Task.FromResult(extension switch
        {
            ".txt" => Text(Encoding.UTF8.GetString(content.Span), 1, 0),
            ".json" => Json(content.Span),
            ".docx" => Docx(content),
            ".pdf" => Pdf(content),
            ".doc" or ".xlsx" or ".xls" => new BrainParseResult(
                string.Empty,
                0,
                0,
                false,
                true,
                "Excel and legacy Word are not supported yet."),
            _ => new BrainParseResult(string.Empty, 0, 0, false, true, "This file type is not supported yet.")
        });
    }

    private static BrainParseResult Text(string text, int pages, int tables)
    {
        var cleaned = text.Trim();
        return new BrainParseResult(cleaned, pages, tables, false, false, null);
    }

    private static BrainParseResult Pdf(ReadOnlyMemory<byte> content)
    {
        using var document = PdfDocument.Open(content.ToArray());
        var builder = new StringBuilder();
        foreach (var page in document.GetPages())
        {
            builder.AppendLine(page.Text);
        }

        var text = builder.ToString().Trim();
        if (text.Length < 20)
        {
            return new BrainParseResult(string.Empty, document.NumberOfPages, 0, true, false, null);
        }

        return new BrainParseResult(text, document.NumberOfPages, 0, false, false, null);
    }

    private static BrainParseResult Docx(ReadOnlyMemory<byte> content)
    {
        using var stream = new MemoryStream(content.ToArray());
        using var document = WordprocessingDocument.Open(stream, false);
        var body = document.MainDocumentPart?.Document.Body;
        if (body is null)
        {
            return new BrainParseResult(string.Empty, 0, 0, false, true, "The Word file has no body.");
        }

        var builder = new StringBuilder();
        var tables = 0;
        foreach (var element in body.Elements())
        {
            if (element is Paragraph paragraph)
            {
                var line = paragraph.InnerText.Trim();
                if (line.Length > 0)
                {
                    builder.AppendLine(line);
                }
            }
            else if (element is Table table)
            {
                tables++;
                foreach (var row in table.Elements<TableRow>())
                {
                    var cells = row.Elements<TableCell>().Select(cell => cell.InnerText.Trim()).ToList();
                    if (cells.Count == 2)
                    {
                        builder.Append(cells[0]).Append(" = ").AppendLine(cells[1]);
                    }
                    else if (cells.Count > 0)
                    {
                        builder.AppendLine(string.Join(" | ", cells));
                    }
                }
            }
        }

        var text = builder.ToString().Trim();
        if (text.Length == 0)
        {
            return new BrainParseResult(string.Empty, 1, tables, false, true, "The Word file has no text.");
        }

        return new BrainParseResult(text, 1, tables, false, false, null);
    }

    private static BrainParseResult Json(ReadOnlySpan<byte> content)
    {
        try
        {
            return Text(JsonToPhrases(content), 1, 0);
        }
        catch (JsonException)
        {
            return new BrainParseResult(string.Empty, 1, 0, false, true, "The JSON file could not be read.");
        }
    }

    private static string JsonToPhrases(ReadOnlySpan<byte> content)
    {
        using var document = JsonDocument.Parse(content.ToArray());
        var builder = new StringBuilder();
        Write(builder, document.RootElement, string.Empty);
        return builder.ToString().Trim();
    }

    private static void Write(StringBuilder builder, JsonElement element, string prefix)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Array:
                foreach (var item in element.EnumerateArray())
                {
                    Write(builder, item, prefix);
                }

                break;
            case JsonValueKind.Object:
                foreach (var property in element.EnumerateObject())
                {
                    var next = string.IsNullOrEmpty(prefix) ? property.Name : $"{prefix} {property.Name}";
                    Write(builder, property.Value, next);
                }

                break;
            default:
                if (!string.IsNullOrEmpty(prefix))
                {
                    builder.Append(prefix).Append(": ");
                }

                builder.AppendLine(element.ToString());
                break;
        }
    }
}
