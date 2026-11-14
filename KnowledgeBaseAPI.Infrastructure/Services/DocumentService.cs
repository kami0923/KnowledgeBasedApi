using KnowledgeBaseAPI.Application.DTOs;
using KnowledgeBaseAPI.Application.Exceptions;
using KnowledgeBaseAPI.Application.Interfaces;
using KnowledgeBaseAPI.Domain.Entities;
using KnowledgeBaseAPI.Domain.Enums;
using KnowledgeBaseAPI.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace KnowledgeBaseAPI.Infrastructure.Services;

public class DocumentService : IDocumentService
{
    private readonly AppDbContext _context;
    private readonly IFileStorageService _fileStorage;
    private readonly ITextExtractionService _textExtraction;
    private readonly ILogger<DocumentService> _logger;

    private static readonly string[] AllowedContentTypes =
    {
        "application/pdf",
        "application/vnd.openxmlformats-officedocument.wordprocessingml.document", // .docx
        "text/plain"
    };

    private const long MaxFileSizeBytes = 10 * 1024 * 1024; // 10 MB

    public DocumentService(
        AppDbContext context,
        IFileStorageService fileStorage,
        ITextExtractionService textExtraction,
        ILogger<DocumentService> logger)
    {
        _context = context;
        _fileStorage = fileStorage;
        _textExtraction = textExtraction;
        _logger = logger;
    }

    public async Task<DocumentResponseDto> UploadAsync(
        Stream fileStream, string originalFileName, string contentType, string? title, int userId)
    {
        if (!AllowedContentTypes.Contains(contentType))
            throw new InvalidOperationException("Only PDF, DOCX, and TXT files are supported.");

        if (fileStream.Length > MaxFileSizeBytes)
            throw new InvalidOperationException("File exceeds the 10 MB size limit.");

        var (storedFileName, fileSizeBytes) =
            await _fileStorage.SaveFileAsync(fileStream, originalFileName, userId);

        var document = new Document
        {
            UserId = userId,
            Title = string.IsNullOrWhiteSpace(title) ? originalFileName : title,
            OriginalFileName = originalFileName,
            StoredFileName = storedFileName,
            ContentType = contentType,
            FileSizeBytes = fileSizeBytes,
            UploadedAt = DateTime.UtcNow,
            Status = DocumentStatus.Processing
        };

        // Save first, so the document exists in the database before extraction starts.
        _context.Documents.Add(document);
        await _context.SaveChangesAsync();

        await ProcessDocumentAsync(document);
        await _context.SaveChangesAsync();

        return MapToDto(document);
    }

    public async Task<List<DocumentResponseDto>> GetAllForUserAsync(int userId)
    {
        // Partial projection: SQL selects ONLY these columns, so the large
        // ExtractedText column is never loaded for a simple list.
        var documents = await _context.Documents
            .AsNoTracking()
            .Where(d => d.UserId == userId)
            .OrderByDescending(d => d.UploadedAt)
            .Select(d => new Document
            {
                Id = d.Id,
                Title = d.Title,
                OriginalFileName = d.OriginalFileName,
                ContentType = d.ContentType,
                FileSizeBytes = d.FileSizeBytes,
                UploadedAt = d.UploadedAt,
                Status = d.Status,
                ExtractedCharacterCount = d.ExtractedCharacterCount,
                ProcessingError = d.ProcessingError,
                ProcessedAt = d.ProcessedAt
            })
            .ToListAsync();

        return documents.Select(MapToDto).ToList();
    }

    public async Task<(Stream stream, string fileName, string contentType)?> DownloadAsync(
        int documentId, int userId)
    {
        var document = await _context.Documents
            .AsNoTracking()
            .Where(d => d.Id == documentId && d.UserId == userId)
            .Select(d => new { d.StoredFileName, d.OriginalFileName, d.ContentType })
            .FirstOrDefaultAsync();

        if (document == null) return null;

        var stream = await _fileStorage.GetFileAsync(document.StoredFileName, userId);
        return (stream, document.OriginalFileName, document.ContentType);
    }

    public async Task<bool> DeleteAsync(int documentId, int userId)
    {
        var document = await _context.Documents
            .FirstOrDefaultAsync(d => d.Id == documentId && d.UserId == userId);

        if (document == null) return false;

        _fileStorage.DeleteFile(document.StoredFileName, userId);
        _context.Documents.Remove(document);
        await _context.SaveChangesAsync();

        return true;
    }

    // Runs extraction and records the outcome on the entity. Never throws.
    private async Task ProcessDocumentAsync(Document document)
    {
        try
        {
            await using var stored = await _fileStorage.GetFileAsync(document.StoredFileName, document.UserId);
            var text = await _textExtraction.ExtractAsync(stored, document.ContentType);

            document.ExtractedText = text;
            document.ExtractedCharacterCount = text.Length;
            document.Status = DocumentStatus.Ready;
            document.ProcessingError = null;
        }
        catch (TextExtractionException ex)
        {
            // Expected failure with a user-safe message (scanned PDF, empty file, corrupt file...)
            _logger.LogWarning(ex, "Text extraction failed for document {DocumentId}", document.Id);
            document.Status = DocumentStatus.Failed;
            document.ProcessingError = ex.Message;
        }
        catch (Exception ex)
        {
            // Unexpected failure: log the details, show the user something generic
            _logger.LogError(ex, "Unexpected error processing document {DocumentId}", document.Id);
            document.Status = DocumentStatus.Failed;
            document.ProcessingError = "Something went wrong while reading this document.";
        }
        finally
        {
            document.ProcessedAt = DateTime.UtcNow;
        }
    }

    private static DocumentResponseDto MapToDto(Document d) => new()
    {
        Id = d.Id,
        Title = d.Title,
        OriginalFileName = d.OriginalFileName,
        ContentType = d.ContentType,
        FileSizeBytes = d.FileSizeBytes,
        UploadedAt = d.UploadedAt,
        Status = d.Status.ToString(),
        ExtractedCharacterCount = d.ExtractedCharacterCount,
        ProcessingError = d.ProcessingError,
        ProcessedAt = d.ProcessedAt
    };
}