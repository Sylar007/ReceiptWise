namespace ReceiptWise.Tests.Unit.Services;

using FluentAssertions;
using ReceiptWise.Services.Helpers;
using Xunit;

public class ImageOptimizationServiceTests
{
    private readonly ImageOptimizationService _service;

    public ImageOptimizationServiceTests()
    {
        _service = new ImageOptimizationService();
    }

    [Fact]
    public async Task CompressImageAsync_Should_Reduce_File_Size()
    {
        // Arrange - Create a simple test image
        var originalStream = CreateTestImage();
        var originalSize = originalStream.Length;

        // Act
        var compressedStream = await _service.CompressImageAsync(originalStream, quality: 50);

        // Assert
        compressedStream.Should().NotBeNull();
        compressedStream.Length.Should().BeLessThan(originalSize);
    }

    [Fact]
    public async Task GenerateThumbnailAsync_Should_Create_Small_Image()
    {
        // Arrange
        var originalStream = CreateTestImage();

        // Act
        var thumbnailStream = await _service.GenerateThumbnailAsync(originalStream, 100);

        // Assert
        thumbnailStream.Should().NotBeNull();
        thumbnailStream.Length.Should().BeGreaterThan(0);
    }

    private Stream CreateTestImage()
    {
        // Create a simple 100x100 red square
        using var bitmap = new SkiaSharp.SKBitmap(100, 100);
        using var canvas = new SkiaSharp.SKCanvas(bitmap);
        canvas.Clear(SkiaSharp.SKColors.Red);

        using var image = SkiaSharp.SKImage.FromBitmap(bitmap);
        using var data = image.Encode(SkiaSharp.SKEncodedImageFormat.Jpeg, 100);

        var stream = new MemoryStream();
        data.SaveTo(stream);
        stream.Position = 0;
        return stream;
    }
}