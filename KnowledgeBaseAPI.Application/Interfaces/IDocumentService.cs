using KnowledgeBaseAPI.Application.DTOs;

namespace KnowledgeBaseAPI.Application.Interfaces;

public interface IDocumentService
{
    Task<DocumentResponseDto> UploadAsync(Stream fileStream, string originalFileName, string contentType, string? title, int userId);
    Task<List<DocumentResponseDto>> GetAllForUserAsync(int userId);
    Task<(Stream stream, string fileName, string contentType)?> DownloadAsync(int documentId, int userId);
    Task<bool> DeleteAsync(int documentId, int userId);

}
