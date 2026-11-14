namespace KnowledgeBaseAPI.Application.DTOs;

public class DocumentResponseDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string OriginalFileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long FileSizeBytes { get; set; }
    public DateTime UploadedAt { get; set; }

    public string Status { get; set; } = string.Empty;
    public int? ExtractedCharacterCount { get; set; }
    public string? ProcessingError { get; set; }
    public DateTime? ProcessedAt { get; set; }
}