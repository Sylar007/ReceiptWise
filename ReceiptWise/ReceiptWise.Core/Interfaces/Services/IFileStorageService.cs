namespace ReceiptWise.Core.Interfaces.Services;

/// <summary>
/// Service for local file storage operations
/// </summary>
public interface IFileStorageService
{
    Task<string> SaveFileAsync(Stream stream, string fileName, CancellationToken cancellationToken = default);

    Task<string> SaveThumbnailAsync(Stream stream, string fileName, CancellationToken cancellationToken = default);

    Task<Stream> GetFileAsync(string filePath, CancellationToken cancellationToken = default);

    Task DeleteFileAsync(string filePath, CancellationToken cancellationToken = default);

    Task<bool> FileExistsAsync(string filePath);

    // Add these new methods
    long GetTotalStorageSize();

    Task ClearAllFilesAsync();
}