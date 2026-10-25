namespace KnowledgeBaseAPI.Application.Interfaces;

public interface IFileStorageService
{
    Task<(string storedFileName, long fileSizeBytes)> SaveFileAsync(Stream fileStream, string originalFileName, int userId);
    Task<Stream> GetFileAsync(string storedFileName, int userId);
    void DeleteFile(string storedFileName, int userId);
}
