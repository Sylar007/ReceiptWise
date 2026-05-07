namespace ReceiptWise.Services.Helpers;

using SkiaSharp;
using Microsoft.Extensions.Logging;

/// <summary>
/// Service for optimizing images using SkiaSharp
/// Compresses, resizes, and generates thumbnails
/// </summary>
public class ImageOptimizationService
{
    private readonly ILogger<ImageOptimizationService>? _logger;

    public ImageOptimizationService(ILogger<ImageOptimizationService>? logger = null)
    {
        _logger = logger;
    }

    /// <summary>
    /// Compress and resize image to maximum dimensions
    /// </summary>
    public async Task<Stream> CompressImageAsync(
        Stream imageStream,
        int maxWidth = 1920,
        int maxHeight = 1920,
        int quality = 85,
        CancellationToken cancellationToken = default)
    {
        return await Task.Run(() =>
        {
            try
            {
                imageStream.Position = 0;

                using var originalBitmap = SKBitmap.Decode(imageStream);

                if (originalBitmap == null)
                {
                    _logger?.LogWarning("Failed to decode image");
                    imageStream.Position = 0;
                    return imageStream;
                }

                // Calculate new dimensions maintaining aspect ratio
                var (newWidth, newHeight) = CalculateScaledDimensions(
                    originalBitmap.Width,
                    originalBitmap.Height,
                    maxWidth,
                    maxHeight);

                // Check if resize is needed
                if (newWidth == originalBitmap.Width && newHeight == originalBitmap.Height)
                {
                    // No resize needed, just compress
                    return CompressWithoutResize(imageStream, quality);
                }

                // Resize and compress
                using var resizedBitmap = originalBitmap.Resize(
                    new SKImageInfo(newWidth, newHeight),
                    SKFilterQuality.High);

                if (resizedBitmap == null)
                {
                    _logger?.LogWarning("Failed to resize image");
                    imageStream.Position = 0;
                    return imageStream;
                }

                using var image = SKImage.FromBitmap(resizedBitmap);
                using var data = image.Encode(SKEncodedImageFormat.Jpeg, quality);

                var outputStream = new MemoryStream();
                data.SaveTo(outputStream);
                outputStream.Position = 0;

                var originalSizeKB = imageStream.Length / 1024;
                var newSizeKB = outputStream.Length / 1024;

                _logger?.LogInformation(
                    "Image compressed from {OriginalSize} KB to {NewSize} KB ({Percent}% reduction)",
                    originalSizeKB,
                    newSizeKB,
                    originalSizeKB > 0 ? (int)((1 - ((double)newSizeKB / originalSizeKB)) * 100) : 0);

                return outputStream;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to compress image");
                imageStream.Position = 0;
                return imageStream;
            }
        }, cancellationToken);
    }

    /// <summary>
    /// Generate thumbnail from image
    /// </summary>
    public async Task<Stream> GenerateThumbnailAsync(
        Stream imageStream,
        int thumbnailSize = 200,
        CancellationToken cancellationToken = default)
    {
        return await Task.Run(() =>
        {
            try
            {
                imageStream.Position = 0;

                using var originalBitmap = SKBitmap.Decode(imageStream);

                if (originalBitmap == null)
                {
                    _logger?.LogWarning("Failed to decode image for thumbnail");
                    imageStream.Position = 0;
                    return imageStream;
                }

                // Calculate square thumbnail dimensions
                var size = Math.Min(originalBitmap.Width, originalBitmap.Height);
                var x = (originalBitmap.Width - size) / 2;
                var y = (originalBitmap.Height - size) / 2;

                // Crop to square
                using var croppedBitmap = new SKBitmap(size, size);
                using var canvas = new SKCanvas(croppedBitmap);
                canvas.DrawBitmap(
                    originalBitmap,
                    new SKRect(x, y, x + size, y + size),
                    new SKRect(0, 0, size, size));

                // Resize to thumbnail size
                using var thumbnailBitmap = croppedBitmap.Resize(
                    new SKImageInfo(thumbnailSize, thumbnailSize),
                    SKFilterQuality.Medium);

                if (thumbnailBitmap == null)
                {
                    _logger?.LogWarning("Failed to resize thumbnail");
                    imageStream.Position = 0;
                    return imageStream;
                }

                using var image = SKImage.FromBitmap(thumbnailBitmap);
                using var data = image.Encode(SKEncodedImageFormat.Jpeg, 80);

                var outputStream = new MemoryStream();
                data.SaveTo(outputStream);
                outputStream.Position = 0;

                _logger?.LogInformation("Thumbnail generated: {Size} bytes", outputStream.Length);

                return outputStream;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to generate thumbnail");
                imageStream.Position = 0;
                return imageStream;
            }
        }, cancellationToken);
    }

    /// <summary>
    /// Auto-rotate image based on EXIF orientation
    /// </summary>
    public async Task<Stream> AutoRotateImageAsync(Stream imageStream)
    {
        return await Task.Run(() =>
        {
            try
            {
                imageStream.Position = 0;

                using var codec = SKCodec.Create(imageStream);
                if (codec == null)
                {
                    imageStream.Position = 0;
                    return imageStream;
                }

                var origin = codec.EncodedOrigin;

                if (origin == SKEncodedOrigin.Default || origin == SKEncodedOrigin.TopLeft)
                {
                    imageStream.Position = 0;
                    return imageStream;
                }

                using var bitmap = SKBitmap.Decode(imageStream);
                if (bitmap == null)
                {
                    imageStream.Position = 0;
                    return imageStream;
                }

                using var rotated = AutoOrient(bitmap, origin);
                using var image = SKImage.FromBitmap(rotated);
                using var data = image.Encode(SKEncodedImageFormat.Jpeg, 90);

                var outputStream = new MemoryStream();
                data.SaveTo(outputStream);
                outputStream.Position = 0;

                _logger?.LogInformation("Image auto-rotated from orientation: {Orientation}", origin);

                return outputStream;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to auto-rotate image");
                imageStream.Position = 0;
                return imageStream;
            }
        });
    }

    /// <summary>
    /// Compress without resizing (just quality reduction)
    /// </summary>
    private Stream CompressWithoutResize(Stream imageStream, int quality)
    {
        try
        {
            imageStream.Position = 0;

            using var bitmap = SKBitmap.Decode(imageStream);
            if (bitmap == null)
            {
                imageStream.Position = 0;
                return imageStream;
            }

            using var image = SKImage.FromBitmap(bitmap);
            using var data = image.Encode(SKEncodedImageFormat.Jpeg, quality);

            var outputStream = new MemoryStream();
            data.SaveTo(outputStream);
            outputStream.Position = 0;

            return outputStream;
        }
        catch
        {
            imageStream.Position = 0;
            return imageStream;
        }
    }

    /// <summary>
    /// Calculate scaled dimensions maintaining aspect ratio
    /// </summary>
    private (int width, int height) CalculateScaledDimensions(
        int originalWidth,
        int originalHeight,
        int maxWidth,
        int maxHeight)
    {
        if (originalWidth <= maxWidth && originalHeight <= maxHeight)
        {
            return (originalWidth, originalHeight);
        }

        var widthRatio = (double)maxWidth / originalWidth;
        var heightRatio = (double)maxHeight / originalHeight;
        var ratio = Math.Min(widthRatio, heightRatio);

        var newWidth = (int)(originalWidth * ratio);
        var newHeight = (int)(originalHeight * ratio);

        return (newWidth, newHeight);
    }

    /// <summary>
    /// Apply orientation transformation
    /// </summary>
    private SKBitmap AutoOrient(SKBitmap bitmap, SKEncodedOrigin origin)
    {
        var (width, height) = origin switch
        {
            SKEncodedOrigin.LeftBottom or SKEncodedOrigin.RightTop or
            SKEncodedOrigin.LeftTop or SKEncodedOrigin.RightBottom => (bitmap.Height, bitmap.Width),
            _ => (bitmap.Width, bitmap.Height)
        };

        var rotated = new SKBitmap(width, height);
        using var canvas = new SKCanvas(rotated);

        switch (origin)
        {
            case SKEncodedOrigin.BottomRight:
                canvas.RotateDegrees(180, width / 2, height / 2);
                break;
            case SKEncodedOrigin.RightTop:
                canvas.Translate(width, 0);
                canvas.RotateDegrees(90);
                break;
            case SKEncodedOrigin.LeftBottom:
                canvas.Translate(0, height);
                canvas.RotateDegrees(270);
                break;
        }

        canvas.DrawBitmap(bitmap, 0, 0);

        return rotated;
    }
}