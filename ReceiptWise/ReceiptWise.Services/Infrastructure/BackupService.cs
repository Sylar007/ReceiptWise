namespace ReceiptWise.Services.Infrastructure;

using Microsoft.Extensions.Logging;
using ReceiptWise.Data.Context;
using System.IO.Compression;
using Microsoft.Maui.Storage;
/// <summary>
/// Service for backing up and restoring the entire database
/// </summary>
public class BackupService
{
    private readonly ReceiptWiseDatabase _database;
    private readonly ILogger<BackupService>? _logger;

    public BackupService(
        ReceiptWiseDatabase database,
        ILogger<BackupService>? logger = null)
    {
        _database = database;
        _logger = logger;
    }

    /// <summary>
    /// Create a complete backup of database and attachments
    /// </summary>
    public async Task<string> CreateBackupAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger?.LogInformation("Starting backup creation");

            var backupFileName = $"ReceiptWise_Backup_{DateTime.Now:yyyyMMdd_HHmmss}.zip";
            var backupPath = Path.Combine(FileSystem.CacheDirectory, backupFileName);

            using var zipArchive = ZipFile.Open(backupPath, ZipArchiveMode.Create);

            // Backup database file
            var dbPath = Path.Combine(FileSystem.AppDataDirectory, "receiptwise.db3");
            if (File.Exists(dbPath))
            {
                zipArchive.CreateEntryFromFile(dbPath, "receiptwise.db3", CompressionLevel.Optimal);
                _logger?.LogInformation("Database file added to backup");
            }

            // Backup attachments folder
            var attachmentsPath = Path.Combine(FileSystem.AppDataDirectory, "attachments");
            if (Directory.Exists(attachmentsPath))
            {
                var files = Directory.GetFiles(attachmentsPath, "*.*", SearchOption.AllDirectories);
                foreach (var file in files)
                {
                    var entryName = $"attachments/{Path.GetFileName(file)}";
                    zipArchive.CreateEntryFromFile(file, entryName, CompressionLevel.Optimal);
                }
                _logger?.LogInformation("Added {Count} attachment files to backup", files.Length);
            }

            // Backup thumbnails folder
            var thumbnailsPath = Path.Combine(FileSystem.AppDataDirectory, "thumbnails");
            if (Directory.Exists(thumbnailsPath))
            {
                var files = Directory.GetFiles(thumbnailsPath, "*.*", SearchOption.AllDirectories);
                foreach (var file in files)
                {
                    var entryName = $"thumbnails/{Path.GetFileName(file)}";
                    zipArchive.CreateEntryFromFile(file, entryName, CompressionLevel.Optimal);
                }
                _logger?.LogInformation("Added {Count} thumbnail files to backup", files.Length);
            }

            // Add metadata
            var metadata = zipArchive.CreateEntry("backup_info.txt");
            using var metadataWriter = new StreamWriter(metadata.Open());
            await metadataWriter.WriteLineAsync($"ReceiptWise Backup");
            await metadataWriter.WriteLineAsync($"Created: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            await metadataWriter.WriteLineAsync($"Version: 1.0");
            await metadataWriter.WriteLineAsync($"Database: receiptwise.db3");

            var fileInfo = new FileInfo(backupPath);
            _logger?.LogInformation("Backup created: {FilePath} ({Size} bytes)", backupPath, fileInfo.Length);

            return backupPath;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to create backup");
            throw;
        }
    }

    /// <summary>
    /// Restore database from backup file
    /// </summary>
    public async Task<bool> RestoreBackupAsync(string backupFilePath, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger?.LogInformation("Starting backup restoration from: {FilePath}", backupFilePath);

            if (!File.Exists(backupFilePath))
            {
                throw new FileNotFoundException("Backup file not found", backupFilePath);
            }

            // Extract to temp directory
            var tempPath = Path.Combine(FileSystem.CacheDirectory, $"restore_{Guid.NewGuid()}");
            Directory.CreateDirectory(tempPath);

            ZipFile.ExtractToDirectory(backupFilePath, tempPath);

            // Restore database
            var restoredDbPath = Path.Combine(tempPath, "receiptwise.db3");
            if (File.Exists(restoredDbPath))
            {
                var targetDbPath = Path.Combine(FileSystem.AppDataDirectory, "receiptwise.db3");

                // Backup current database first
                if (File.Exists(targetDbPath))
                {
                    var backupCurrent = targetDbPath + ".backup";
                    File.Copy(targetDbPath, backupCurrent, true);
                    _logger?.LogInformation("Current database backed up to: {Path}", backupCurrent);
                }

                File.Copy(restoredDbPath, targetDbPath, true);
                _logger?.LogInformation("Database restored successfully");
            }

            // Restore attachments
            var restoredAttachmentsPath = Path.Combine(tempPath, "attachments");
            if (Directory.Exists(restoredAttachmentsPath))
            {
                var targetAttachmentsPath = Path.Combine(FileSystem.AppDataDirectory, "attachments");
                Directory.CreateDirectory(targetAttachmentsPath);

                var files = Directory.GetFiles(restoredAttachmentsPath);
                foreach (var file in files)
                {
                    var fileName = Path.GetFileName(file);
                    var targetPath = Path.Combine(targetAttachmentsPath, fileName);
                    File.Copy(file, targetPath, true);
                }
                _logger?.LogInformation("Restored {Count} attachment files", files.Length);
            }

            // Restore thumbnails
            var restoredThumbnailsPath = Path.Combine(tempPath, "thumbnails");
            if (Directory.Exists(restoredThumbnailsPath))
            {
                var targetThumbnailsPath = Path.Combine(FileSystem.AppDataDirectory, "thumbnails");
                Directory.CreateDirectory(targetThumbnailsPath);

                var files = Directory.GetFiles(restoredThumbnailsPath);
                foreach (var file in files)
                {
                    var fileName = Path.GetFileName(file);
                    var targetPath = Path.Combine(targetThumbnailsPath, fileName);
                    File.Copy(file, targetPath, true);
                }
                _logger?.LogInformation("Restored {Count} thumbnail files", files.Length);
            }

            // Cleanup temp directory
            Directory.Delete(tempPath, true);

            _logger?.LogInformation("Backup restoration completed successfully");
            return await Task.FromResult(true);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to restore backup");
            throw;
        }
    }

    /// <summary>
    /// Get backup file information
    /// </summary>
    public async Task<BackupInfo?> GetBackupInfoAsync(string backupFilePath)
    {
        try
        {
            if (!File.Exists(backupFilePath))
                return null;

            using var zipArchive = ZipFile.OpenRead(backupFilePath);

            var infoEntry = zipArchive.GetEntry("backup_info.txt");
            if (infoEntry == null)
                return null;

            using var reader = new StreamReader(infoEntry.Open());
            var content = await reader.ReadToEndAsync();

            var fileInfo = new FileInfo(backupFilePath);

            return new BackupInfo
            {
                FilePath = backupFilePath,
                FileName = Path.GetFileName(backupFilePath),
                FileSize = fileInfo.Length,
                CreatedDate = fileInfo.CreationTime,
                EntryCount = zipArchive.Entries.Count
            };
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to get backup info");
            return null;
        }
    }
}

/// <summary>
/// Information about a backup file
/// </summary>
public class BackupInfo
{
    public string FilePath { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public DateTime CreatedDate { get; set; }
    public int EntryCount { get; set; }
}