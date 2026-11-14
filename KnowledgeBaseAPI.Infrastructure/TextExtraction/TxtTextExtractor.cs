using System.Text;
using KnowledgeBaseAPI.Application.Exceptions;
using KnowledgeBaseAPI.Application.Interfaces;

namespace KnowledgeBaseAPI.Infrastructure.TextExtraction;

public class TxtTextExtractor : ITextExtractor
{
    // StartsWith, because browsers may send "text/plain; charset=utf-8".
    public bool CanHandle(string contentType) =>
        contentType.StartsWith("text/plain", StringComparison.OrdinalIgnoreCase);

    public async Task<string> ExtractTextAsync(
        Stream fileStream,
        CancellationToken cancellationToken = default)
    {
        using var reader = new StreamReader(
            fileStream,
            Encoding.UTF8,
            detectEncodingFromByteOrderMarks: true,
            leaveOpen: true);

        var text = await reader.ReadToEndAsync(cancellationToken);

        if (string.IsNullOrWhiteSpace(text))
            throw new TextExtractionException("This text file is empty.");

        return text;
    }
}