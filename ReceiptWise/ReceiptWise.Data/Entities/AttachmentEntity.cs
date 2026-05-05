namespace ReceiptWise.Data.Entities;

using SQLite;

/// <summary>
/// SQLite entity for Attachments table
/// </summary>
[Table("Attachments")]
public class AttachmentEntity
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    [Indexed, Unique]
    public int ReceiptId { get; set; }

    [MaxLength(255)]
    public string FileName { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string FilePath { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string? ThumbnailPath { get; set; }

    [MaxLength(50)]
    public string FileType { get; set; } = string.Empty;

    public long FileSizeBytes { get; set; }

    public DateTime CreatedAt { get; set; }
}