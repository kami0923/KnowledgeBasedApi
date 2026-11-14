using System.Text;
using KnowledgeBaseAPI.Application.Exceptions;
using KnowledgeBaseAPI.Application.Interfaces;
using UglyToad.PdfPig;

namespace KnowledgeBaseAPI.Infrastructure.TextExtraction;

public class PdfTextExtractor : ITextExtractor
{
    public bool CanHandle(string contentType) =>
        string.Equals(contentType, "application/pdf", StringComparison.OrdinalIgnoreCase);

    public async Task<string> ExtractTextAsync(
        Stream fileStream,
        CancellationToken cancellationToken = default)
    {
        // PdfPig needs a seekable stream, so copy the file into memory first.
        using var memory = new MemoryStream();
        await fileStream.CopyToAsync(memory, cancellationToken);
        memory.Position = 0;

        string text;
        try
        {
            using var document = PdfDocument.Open(memory);
            var builder = new StringBuilder();

            foreach (var page in document.GetPages())
            {
                cancellationToken.ThrowIfCancellationRequested();
                builder.AppendLine(page.Text);
                builder.AppendLine();
            }

            text = builder.ToString();
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new TextExtractionException(
                "This PDF could not be read. It may be corrupted or password-protected.", ex);
        }

        if (string.IsNullOrWhiteSpace(text))
            throw new TextExtractionException(
                "No readable text was found in this PDF. It may be a scanned image, which is not supported yet.");

        return text;
    }
}