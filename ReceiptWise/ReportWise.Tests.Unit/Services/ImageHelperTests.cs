namespace ReceiptWise.Tests.Unit.Services;

using FluentAssertions;
using ReceiptWise.Services.Helpers;
using Xunit;

public class ImageHelperTests
{
    private readonly ImageHelper _helper;

    public ImageHelperTests()
    {
        _helper = new ImageHelper();
    }

    [Theory]
    [InlineData(".jpg", "image/jpeg")]
    [InlineData(".jpeg", "image/jpeg")]
    [InlineData(".png", "image/png")]
    [InlineData(".gif", "image/gif")]
    [InlineData(".pdf", "application/pdf")]
    [InlineData(".unknown", "application/octet-stream")]
    public void GetMimeType_Should_Return_Correct_Type(string extension, string expectedMimeType)
    {
        // Act
        var mimeType = ImageHelper.GetMimeType($"file{extension}");

        // Assert
        mimeType.Should().Be(expectedMimeType);
    }

    [Fact]
    public void IsValidImage_Should_Return_True_For_JPEG()
    {
        // Arrange - JPEG header: FF D8 FF
        var jpegHeader = new byte[] { 0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10, 0x4A, 0x46 };
        var stream = new MemoryStream(jpegHeader);

        // Act
        var isValid = _helper.IsValidImage(stream);

        // Assert
        isValid.Should().BeTrue();
    }

    [Fact]
    public void IsValidImage_Should_Return_True_For_PNG()
    {
        // Arrange - PNG header: 89 50 4E 47
        var pngHeader = new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };
        var stream = new MemoryStream(pngHeader);

        // Act
        var isValid = _helper.IsValidImage(stream);

        // Assert
        isValid.Should().BeTrue();
    }

    [Fact]
    public void IsValidImage_Should_Return_False_For_Invalid_Data()
    {
        // Arrange
        var invalidData = new byte[] { 0x00, 0x00, 0x00, 0x00 };
        var stream = new MemoryStream(invalidData);

        // Act
        var isValid = _helper.IsValidImage(stream);

        // Assert
        isValid.Should().BeFalse();
    }

    [Fact]
    public async Task GenerateThumbnailAsync_Should_Return_Stream()
    {
        // Arrange
        var imageData = new byte[] { 0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10 };
        var stream = new MemoryStream(imageData);

        // Act
        var thumbnailStream = await _helper.GenerateThumbnailAsync(stream);

        // Assert
        thumbnailStream.Should().NotBeNull();
        thumbnailStream.Length.Should().BeGreaterThan(0);
    }
}