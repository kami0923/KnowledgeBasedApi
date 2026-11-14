namespace KnowledgeBaseAPI.Application.Interfaces;

/// <summary>
/// Extracts plain text from ONE kind of file (PDF, DOCX, or TXT).
/// </summary>
public interface ITextExtractor
{
    /// <summary>Returns true if this extractor understands the given MIME type.</summary>
    bool CanHandle(string contentType);

    /// <summary>Reads the file stream and returns its plain text.</summary>
    Task<string> ExtractTextAsync(Stream fileStream, CancellationToken cancellationToken = default);
}