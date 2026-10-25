using KnowledgeBaseAPI.Application.DTOs;
using KnowledgeBaseAPI.Application.Interfaces;
using KnowledgeBaseAPI.Domain.Entities;
using KnowledgeBaseAPI.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace KnowledgeBaseAPI.Infrastructure.Services;

public class DocumentService : IDocumentService
{
    private readonly AppDbContext _context;
    private readonly IFileStorageService _fileStorage;

    private static readonly string[] AllowedContentTypes =
    {
        "application/pdf",
        "application/vnd.openxmlformats-officedocument.wordprocessingml.document", // .docx
        "text/plain"
    };

    private const long MaxFileSizeBytes = 10 * 1024 * 1024; // 10 MB

    public DocumentService(AppDbContext context, IFileStorageService fileStorage)
    {
        _context = context;
        _fileStorage = fileStorage;
    }

    public async Task<DocumentResponseDto> UploadAsync(Stream fileStream, string originalFileName, string contentType, string? title, int userId)
    {
        if (!AllowedContentTypes.Contains(contentType))
            throw new InvalidOperationException("Only PDF, DOCX, and TXT files are supported.");

        if (fileStream.Length > MaxFileSizeBytes)
            throw new InvalidOperationException("File exceeds the 10 MB size limit.");

        var (storedFileName, fileSizeBytes) = await _fileStorage.SaveFileAsync(fileStream, originalFileName, userId);

        var document = new Document
        {
            UserId = userId,
            Title = string.IsNullOrWhiteSpace(title) ? originalFileName : title,
            OriginalFileName = originalFileName,
            StoredFileName = storedFileName,
            ContentType = contentType,
            FileSizeBytes = fileSizeBytes,
            UploadedAt = DateTime.UtcNow
        };

        _context.Documents.Add(document);
        await _context.SaveChangesAsync();

        return MapToDto(document);
    }

    public async Task<List<DocumentResponseDto>> GetAllForUserAsync(int userId)
    {
        var documents = await _context.Documents
            .Where(d => d.UserId == userId)
            .OrderByDescending(d => d.UploadedAt)
            .ToListAsync();

        return documents.Select(MapToDto).ToList();
    }

    public async Task<(Stream stream, string fileName, string contentType)?> DownloadAsync(int documentId, int userId)
    {
        var document = await _context.Documents
            .FirstOrDefaultAsync(d => d.Id == documentId && d.UserId == userId);

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

    private static DocumentResponseDto MapToDto(Document d) => new()
    {
        Id = d.Id,
        Title = d.Title,
        OriginalFileName = d.OriginalFileName,
        ContentType = d.ContentType,
        FileSizeBytes = d.FileSizeBytes,
        UploadedAt = d.UploadedAt
    };
}