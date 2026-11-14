namespace KnowledgeBaseAPI.Application.Exceptions;

/// <summary>
/// Thrown when text cannot be extracted from a document.
/// The message is written to be safe to show to the end user.
/// </summary>
public class TextExtractionException : Exception
{
    public TextExtractionException(string message) : base(message)
    {
    }

    public TextExtractionException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}