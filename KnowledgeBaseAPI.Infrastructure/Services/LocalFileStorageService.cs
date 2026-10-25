using KnowledgeBaseAPI.Application.Interfaces;
using Microsoft.Extensions.Configuration;

namespace KnowledgeBaseAPI.Infrastructure.Services;

public class LocalFileStorageService : IFileStorageService
{
    private readonly string _rootPath;

    public LocalFileStorageService(IConfiguration configuration)
    {
        // Reads from appsettings.json, defaults to a local "UploadedFiles" folder if not set
        _rootPath = configuration["FileStorage:RootPath"] ?? "UploadedFiles";
    }

    public async Task<(string storedFileName, long fileSizeBytes)> SaveFileAsync(Stream fileStream, string originalFileName, int userId)
    {
        var userFolder = Path.Combine(_rootPath, userId.ToString());
        Directory.CreateDirectory(userFolder); // creates the folder if it doesn't exist, no-ops if it does

        var extension = Path.GetExtension(originalFileName);
        var storedFileName = $"{Guid.NewGuid()}{extension}";
        var fullPath = Path.Combine(userFolder, storedFileName);

        using (var fileOutput = new FileStream(fullPath, FileMode.Create))
        {
            await fileStream.CopyToAsync(fileOutput);
        }

        var fileInfo = new FileInfo(fullPath);
        return (storedFileName, fileInfo.Length);
    }


    public Task<Stream> GetFileAsync(string storedFileName, int userId)
    {
        var fullPath = Path.Combine(_rootPath, userId.ToString(), storedFileName);

        if (!File.Exists(fullPath))
            throw new FileNotFoundException("The requested file could not be found.");

        Stream stream = new FileStream(fullPath, FileMode.Open, FileAccess.Read);
        return Task.FromResult(stream);
    }

    public void DeleteFile(string storedFileName, int userId)
    {
        var fullPath = Path.Combine(_rootPath, userId.ToString(), storedFileName);
        if (File.Exists(fullPath))  
            File.Delete(fullPath);
    }
}