namespace ReceiptWise.Tests.Unit.Services;

using FluentAssertions;
using ReceiptWise.Core.Interfaces.Services;
using ReceiptWise.Services.Infrastructure;
using Xunit;

public class FileStorageServiceTests : IDisposable
{
    private readonly FileStorageService _service;
    private readonly string _testDataPath;

    public FileStorageServiceTests()
    {
        _testDataPath = Path.Combine(Path.GetTempPath(), $"receiptwise_test_{Guid.NewGuid()}");
        Directory.CreateDirectory(_testDataPath);

        // Mock FileSystem.AppDataDirectory for testing
        _service = new FileStorageService();
    }

    [Fact]
    public async Task SaveFileAsync_Should_Save_File_And_Return_Path()
    {
        // Arrange
        var content = "Test receipt image content"u8.ToArray();
        var stream = new MemoryStream(content);
        var fileName = "test_receipt.jpg";

        // Act
        var filePath = await _service.SaveFileAsync(stream, fileName);

        // Assert
        filePath.Should().NotBeNullOrEmpty();
        File.Exists(filePath).Should().BeTrue();

        var savedContent = await File.ReadAllBytesAsync(filePath);
        savedContent.Should().BeEquivalentTo(content);
    }

    [Fact]
    public async Task SaveFileAsync_Should_Generate_Unique_Filenames()
    {
        // Arrange
        var content = "Test content"u8.ToArray();
        var stream1 = new MemoryStream(content);
        var stream2 = new MemoryStream(content);
        var fileName = "receipt.jpg";

        // Act
        var path1 = await _service.SaveFileAsync(stream1, fileName);
        var path2 = await _service.SaveFileAsync(stream2, fileName);

        // Assert
        path1.Should().NotBe(path2);
    }

    [Fact]
    public async Task SaveThumbnailAsync_Should_Save_Thumbnail()
    {
        // Arrange
        var content = "Thumbnail content"u8.ToArray();
        var stream = new MemoryStream(content);
        var fileName = "receipt_thumb.jpg";

        // Act
        var thumbnailPath = await _service.SaveThumbnailAsync(stream, fileName);

        // Assert
        thumbnailPath.Should().NotBeNullOrEmpty();
        thumbnailPath.Should().Contain("thumb_");
        File.Exists(thumbnailPath).Should().BeTrue();
    }

    [Fact]
    public async Task GetFileAsync_Should_Return_File_Stream()
    {
        // Arrange
        var content = "Test content"u8.ToArray();
        var saveStream = new MemoryStream(content);
        var filePath = await _service.SaveFileAsync(saveStream, "test.jpg");

        // Act
        var retrievedStream = await _service.GetFileAsync(filePath);
        var retrievedContent = new byte[retrievedStream.Length];
        await retrievedStream.ReadAsync(retrievedContent);

        // Assert
        retrievedContent.Should().BeEquivalentTo(content);
    }

    [Fact]
    public async Task DeleteFileAsync_Should_Remove_File()
    {
        // Arrange
        var content = "Test content"u8.ToArray();
        var stream = new MemoryStream(content);
        var filePath = await _service.SaveFileAsync(stream, "delete_test.jpg");

        // Act
        await _service.DeleteFileAsync(filePath);

        // Assert
        File.Exists(filePath).Should().BeFalse();
    }

    [Fact]
    public async Task FileExistsAsync_Should_Return_True_For_Existing_File()
    {
        // Arrange
        var content = "Test content"u8.ToArray();
        var stream = new MemoryStream(content);
        var filePath = await _service.SaveFileAsync(stream, "exists_test.jpg");

        // Act
        var exists = await _service.FileExistsAsync(filePath);

        // Assert
        exists.Should().BeTrue();
    }

    [Fact]
    public async Task FileExistsAsync_Should_Return_False_For_NonExistent_File()
    {
        // Act
        var exists = await _service.FileExistsAsync("/fake/path/nonexistent.jpg");

        // Assert
        exists.Should().BeFalse();
    }

    public void Dispose()
    {
        // Cleanup test directory
        if (Directory.Exists(_testDataPath))
        {
            Directory.Delete(_testDataPath, true);
        }
        GC.SuppressFinalize(this);
    }
}