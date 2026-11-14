using System.Text;
using System.Text.RegularExpressions;
using KnowledgeBaseAPI.Application.Exceptions;
using KnowledgeBaseAPI.Application.Interfaces;

namespace KnowledgeBaseAPI.Infrastructure.TextExtraction;

public class TextExtractionService : ITextExtractionService
{
    // Safety guard against absurdly large output (about 500k tokens).
    private const int MaxCharacters = 2_000_000;

    private static readonly Regex RepeatedSpaces = new(@"[ \t]+", RegexOptions.Compiled);
    private static readonly Regex SpacesAroundNewlines = new(@" ?\n ?", RegexOptions.Compiled);
    private static readonly Regex ManyBlankLines = new(@"\n{3,}", RegexOptions.Compiled);

    private readonly IEnumerable<ITextExtractor> _extractors;

    public TextExtractionService(IEnumerable<ITextExtractor> extractors)
    {
        _extractors = extractors;
    }

    public async Task<string> ExtractAsync(
        Stream fileStream,
        string contentType,
        CancellationToken cancellationToken = default)
    {
        var extractor = _extractors.FirstOrDefault(e => e.CanHandle(contentType));

        if (extractor == null)
            throw new TextExtractionException("This file type is not supported for text extraction.");

        var rawText = await extractor.ExtractTextAsync(fileStream, cancellationToken);
        var cleaned = Clean(rawText);

        if (string.IsNullOrWhiteSpace(cleaned))
            throw new TextExtractionException("No readable text was found in this document.");

        if (cleaned.Length > MaxCharacters)
            throw new TextExtractionException(
                "This document contains too much text to process. Try splitting it into smaller files.");

        return cleaned;
    }

    private static string Clean(string text)
    {
        // 1. Drop control characters (null bytes, form feeds, etc.) but keep newlines and tabs.
        var builder = new StringBuilder(text.Length);
        foreach (var c in text)
        {
            if (c == '\n' || c == '\r' || c == '\t' || !char.IsControl(c))
                builder.Append(c);
        }

        var result = builder.ToString();

        // 2. Normalize line endings and non-breaking spaces.
        result = result.Replace("\r\n", "\n").Replace('\r', '\n').Replace('\u00A0', ' ');

        // 3. Collapse whitespace noise.
        result = RepeatedSpaces.Replace(result, " ");
        result = SpacesAroundNewlines.Replace(result, "\n");
        result = ManyBlankLines.Replace(result, "\n\n");

        return result.Trim();
    }
}