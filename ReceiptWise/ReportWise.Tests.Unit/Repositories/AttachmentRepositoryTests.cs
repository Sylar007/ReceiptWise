namespace ReceiptWise.Tests.Unit.Repositories;

using FluentAssertions;
using ReceiptWise.Core.Models.Domain;
using ReceiptWise.Data.Repositories;
using Xunit;

public class AttachmentRepositoryTests : TestBase
{
    private readonly AttachmentRepository _repository;

    public AttachmentRepositoryTests()
    {
        _repository = new AttachmentRepository(Database);
    }

    [Fact]
    public async Task AddAsync_Should_Insert_Attachment()
    {
        // Arrange
        var attachment = new Attachment
        {
            ReceiptId = 1,
            FileName = "receipt.jpg",
            FilePath = "/data/attachments/receipt.jpg",
            ThumbnailPath = "/data/thumbnails/receipt_thumb.jpg",
            FileType = "image/jpeg",
            FileSizeBytes = 524288 // 512 KB
        };

        // Act
        var attachmentId = await _repository.AddAsync(attachment);

        // Assert
        attachmentId.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task GetByReceiptIdAsync_Should_Return_Attachment()
    {
        // Arrange
        var attachment = new Attachment
        {
            ReceiptId = 1,
            FileName = "receipt.pdf",
            FilePath = "/data/attachments/receipt.pdf",
            FileType = "application/pdf",
            FileSizeBytes = 1048576 // 1 MB
        };
        await _repository.AddAsync(attachment);

        // Act
        var result = await _repository.GetByReceiptIdAsync(1);

        // Assert
        result.Should().NotBeNull();
        result!.FileName.Should().Be("receipt.pdf");
        result.FileType.Should().Be("application/pdf");
        result.FileSizeBytes.Should().Be(1048576);
    }

    [Fact]
    public async Task GetByReceiptIdAsync_Should_Return_Null_If_Not_Found()
    {
        // Act
        var result = await _repository.GetByReceiptIdAsync(999);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task DeleteAsync_Should_Remove_Attachment()
    {
        // Arrange
        var attachment = new Attachment
        {
            ReceiptId = 1,
            FileName = "receipt.jpg",
            FilePath = "/data/attachments/receipt.jpg",
            FileType = "image/jpeg",
            FileSizeBytes = 524288
        };
        var attachmentId = await _repository.AddAsync(attachment);

        // Act
        await _repository.DeleteAsync(attachmentId);
        var result = await _repository.GetByReceiptIdAsync(1);

        // Assert
        result.Should().BeNull();
    }
}