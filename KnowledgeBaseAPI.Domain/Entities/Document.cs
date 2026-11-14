using KnowledgeBaseAPI.Domain.Enums;

namespace KnowledgeBaseAPI.Domain.Entities;

public class Document
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public User User { get; set; } = null!;

    public string Title { get; set; } = string.Empty;
    public string OriginalFileName { get; set; } = string.Empty;
    public string StoredFileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long FileSizeBytes { get; set; }
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

    // Text extraction (Day 6)
    public DocumentStatus Status { get; set; } = DocumentStatus.Pending;
    public string? ExtractedText { get; set; }
    public int? ExtractedCharacterCount { get; set; }
    public string? ProcessingError { get; set; }
    public DateTime? ProcessedAt { get; set; }
}