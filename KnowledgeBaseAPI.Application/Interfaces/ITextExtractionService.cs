namespace KnowledgeBaseAPI.Application.Interfaces;

/// <summary>
/// The single entry point the rest of the app uses. It picks the right
/// ITextExtractor for the file type, so callers never need to know which one.
/// </summary>
public interface ITextExtractionService
{
    Task<string> ExtractAsync(
        Stream fileStream,
        string contentType,
        CancellationToken cancellationToken = default);
}