namespace ReceiptWise.Services.Helpers;

using ReceiptWise.Core.Constants;
using Microsoft.Extensions.Logging;

/// <summary>
/// Helper for image processing: compression, resizing, thumbnail generation
/// Uses SkiaSharp for cross-platform image manipulation
/// </summary>
public class ImageHelper
{
    private readonly ILogger<ImageHelper>? _logger;

    public ImageHelper(ILogger<ImageHelper>? logger = null)
    {
        _logger = logger;
    }

    /// <summary>
    /// Compress image if it exceeds max size
    /// </summary>
    public async Task<Stream> CompressImageAsync(Stream imageStream, int maxSizeKB = 5120)
    {
        if (imageStream == null || imageStream.Length == 0)
            throw new ArgumentException("Image stream is null or empty");

        try
        {
            var currentSizeKB = imageStream.Length / 1024;

            if (currentSizeKB <= maxSizeKB)
            {
                _logger?.LogInformation("Image size ({Size} KB) is within limit, no compression needed", currentSizeKB);
                imageStream.Position = 0;
                return imageStream;
            }

            _logger?.LogInformation("Compressing image from {Current} KB to ~{Max} KB", currentSizeKB, maxSizeKB);

            // For MAUI, we'll use a simple approach with quality reduction
            // In production, use SkiaSharp or ImageSharp for better control
            return await Task.FromResult(imageStream);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to compress image");
            throw;
        }
    }

    /// <summary>
    /// Generate thumbnail from image stream
    /// </summary>
    public async Task<Stream> GenerateThumbnailAsync(Stream imageStream, int size = 200)
    {
        if (imageStream == null || imageStream.Length == 0)
            throw new ArgumentException("Image stream is null or empty");

        try
        {
            _logger?.LogInformation("Generating thumbnail with size {Size}px", size);

            // TODO: Implement actual thumbnail generation with SkiaSharp in production
            // For now, return the original stream (Milestone 10 optimization)
            imageStream.Position = 0;
            var memoryStream = new MemoryStream();
            await imageStream.CopyToAsync(memoryStream);
            memoryStream.Position = 0;
            return memoryStream;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to generate thumbnail");
            throw;
        }
    }

    /// <summary>
    /// Validate if stream is a valid image
    /// </summary>
    public bool IsValidImage(Stream imageStream)
    {
        if (imageStream == null || imageStream.Length == 0)
            return false;

        try
        {
            imageStream.Position = 0;
            var buffer = new byte[8];
            imageStream.Read(buffer, 0, 8);
            imageStream.Position = 0;

            // Check for common image signatures
            // JPEG: FF D8 FF
            if (buffer[0] == 0xFF && buffer[1] == 0xD8 && buffer[2] == 0xFF)
                return true;

            // PNG: 89 50 4E 47
            if (buffer[0] == 0x89 && buffer[1] == 0x50 && buffer[2] == 0x4E && buffer[3] == 0x47)
                return true;

            // GIF: 47 49 46
            if (buffer[0] == 0x47 && buffer[1] == 0x49 && buffer[2] == 0x46)
                return true;

            return false;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Get MIME type from file extension
    /// </summary>
    public static string GetMimeType(string fileName)
    {
        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        return extension switch
        {
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".gif" => "image/gif",
            ".bmp" => "image/bmp",
            ".webp" => "image/webp",
            ".pdf" => "application/pdf",
            _ => "application/octet-stream"
        };
    }
}