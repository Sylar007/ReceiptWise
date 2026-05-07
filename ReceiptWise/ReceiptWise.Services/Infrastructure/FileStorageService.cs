namespace ReceiptWise.Services.Infrastructure;

using ReceiptWise.Core.Interfaces.Services;
using ReceiptWise.Core.Exceptions;
using ReceiptWise.Core.Constants;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.Storage;
/// <summary>
/// Service for managing local file storage (images and PDFs)
/// Stores files in app's private storage with optional compression
/// </summary>
public class FileStorageService : IFileStorageService
{
    private readonly ILogger<FileStorageService>? _logger;
    private readonly string _attachmentsPath;
    private readonly string _thumbnailsPath;

    public FileStorageService(ILogger<FileStorageService>? logger = null)
    {
        _logger = logger;

        // Create directories in app data folder
        var appDataPath = FileSystem.AppDataDirectory;
        _attachmentsPath = Path.Combine(appDataPath, AppConstants.Storage.AttachmentsFolder);
        _thumbnailsPath = Path.Combine(appDataPath, AppConstants.Storage.ThumbnailsFolder);

        EnsureDirectoriesExist();
    }

    private void EnsureDirectoriesExist()
    {
        if (!Directory.Exists(_attachmentsPath))
        {
            Directory.CreateDirectory(_attachmentsPath);
            _logger?.LogInformation("Created attachments directory: {Path}", _attachmentsPath);
        }

        if (!Directory.Exists(_thumbnailsPath))
        {
            Directory.CreateDirectory(_thumbnailsPath);
            _logger?.LogInformation("Created thumbnails directory: {Path}", _thumbnailsPath);
        }
    }

    public async Task<string> SaveFileAsync(Stream stream, string fileName, CancellationToken cancellationToken = default)
    {
        if (stream == null || stream.Length == 0)
            throw new StorageException("Stream is null or empty");

        try
        {
            // Generate unique filename to avoid collisions
            var uniqueFileName = $"{Guid.NewGuid()}_{SanitizeFileName(fileName)}";
            var filePath = Path.Combine(_attachmentsPath, uniqueFileName);

            _logger?.LogInformation("Saving file: {FileName} to {Path}", fileName, filePath);

            using var fileStream = File.Create(filePath);
            stream.Position = 0;
            await stream.CopyToAsync(fileStream, cancellationToken);

            _logger?.LogInformation("File saved successfully: {Path} ({Size} bytes)",
                filePath, new FileInfo(filePath).Length);

            return filePath;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to save file: {FileName}", fileName);
            throw new StorageException($"Failed to save file: {fileName}", ex);
        }
    }

    public async Task<string> SaveThumbnailAsync(Stream stream, string fileName, CancellationToken cancellationToken = default)
    {
        if (stream == null || stream.Length == 0)
            throw new StorageException("Stream is null or empty");

        try
        {
            var uniqueFileName = $"thumb_{Guid.NewGuid()}_{SanitizeFileName(fileName)}";
            var filePath = Path.Combine(_thumbnailsPath, uniqueFileName);

            _logger?.LogInformation("Saving thumbnail: {FileName} to {Path}", fileName, filePath);

            using var fileStream = File.Create(filePath);
            stream.Position = 0;
            await stream.CopyToAsync(fileStream, cancellationToken);

            return filePath;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to save thumbnail: {FileName}", fileName);
            throw new StorageException($"Failed to save thumbnail: {fileName}", ex);
        }
    }

    public async Task<Stream> GetFileAsync(string filePath, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            throw new ArgumentException("File path cannot be null or empty", nameof(filePath));

        if (!File.Exists(filePath))
            throw new StorageException($"File not found: {filePath}");

        try
        {
            var memoryStream = new MemoryStream();
            using var fileStream = File.OpenRead(filePath);
            await fileStream.CopyToAsync(memoryStream, cancellationToken);
            memoryStream.Position = 0;
            return memoryStream;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to read file: {FilePath}", filePath);
            throw new StorageException($"Failed to read file: {filePath}", ex);
        }
    }

    public async Task DeleteFileAsync(string filePath, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            return;

        try
        {
            if (File.Exists(filePath))
            {
                await Task.Run(() => File.Delete(filePath), cancellationToken);
                _logger?.LogInformation("Deleted file: {FilePath}", filePath);
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to delete file: {FilePath}", filePath);
            throw new StorageException($"Failed to delete file: {filePath}", ex);
        }
    }

    public Task<bool> FileExistsAsync(string filePath)
    {
        return Task.FromResult(!string.IsNullOrWhiteSpace(filePath) && File.Exists(filePath));
    }

    private static string SanitizeFileName(string fileName)
    {
        var invalidChars = Path.GetInvalidFileNameChars();
        var sanitized = string.Join("_", fileName.Split(invalidChars));
        return sanitized;
    }

    /// <summary>
    /// Get total storage size in bytes
    /// </summary>
    public long GetTotalStorageSize()
    {
        long totalSize = 0;

        try
        {
            if (Directory.Exists(_attachmentsPath))
            {
                var attachmentFiles = Directory.GetFiles(_attachmentsPath);
                totalSize += attachmentFiles.Sum(f => new FileInfo(f).Length);
            }

            if (Directory.Exists(_thumbnailsPath))
            {
                var thumbnailFiles = Directory.GetFiles(_thumbnailsPath);
                totalSize += thumbnailFiles.Sum(f => new FileInfo(f).Length);
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to calculate storage size");
        }

        return totalSize;
    }

    /// <summary>
    /// Clear all stored files (for testing/cleanup)
    /// </summary>
    public async Task ClearAllFilesAsync()
    {
        await Task.Run(() =>
        {
            try
            {
                if (Directory.Exists(_attachmentsPath))
                {
                    foreach (var file in Directory.GetFiles(_attachmentsPath))
                    {
                        File.Delete(file);
                    }
                }

                if (Directory.Exists(_thumbnailsPath))
                {
                    foreach (var file in Directory.GetFiles(_thumbnailsPath))
                    {
                        File.Delete(file);
                    }
                }

                _logger?.LogWarning("All files cleared from storage");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to clear all files");
                throw new StorageException("Failed to clear all files", ex);
            }
        });
    }
}