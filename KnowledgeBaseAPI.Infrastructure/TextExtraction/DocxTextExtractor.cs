using System.Text;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using KnowledgeBaseAPI.Application.Exceptions;
using KnowledgeBaseAPI.Application.Interfaces;

namespace KnowledgeBaseAPI.Infrastructure.TextExtraction;

public class DocxTextExtractor : ITextExtractor
{
    private const string DocxContentType =
        "application/vnd.openxmlformats-officedocument.wordprocessingml.document";

    public bool CanHandle(string contentType) =>
        string.Equals(contentType, DocxContentType, StringComparison.OrdinalIgnoreCase);

    public async Task<string> ExtractTextAsync(
        Stream fileStream,
        CancellationToken cancellationToken = default)
    {
        // A .docx is a zip archive, which also needs a seekable stream.
        using var memory = new MemoryStream();
        await fileStream.CopyToAsync(memory, cancellationToken);
        memory.Position = 0;

        string text;
        try
        {
            using var document = WordprocessingDocument.Open(memory, false);
            var body = document.MainDocumentPart?.Document?.Body;
            var builder = new StringBuilder();

            if (body != null)
            {
                // Descendants also finds paragraphs inside table cells, in document order.
                foreach (var paragraph in body.Descendants<Paragraph>())
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    builder.AppendLine(paragraph.InnerText);
                }
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
                "This Word document could not be read. It may be corrupted or password-protected.", ex);
        }

        if (string.IsNullOrWhiteSpace(text))
            throw new TextExtractionException("No readable text was found in this Word document.");

        return text;
    }
}