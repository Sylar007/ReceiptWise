namespace ReceiptWise.Core.Models.Domain;

/// <summary>
/// Receipt attachment (image or PDF)
/// </summary>
public class Attachment
{
    public int Id { get; set; }

    public int ReceiptId { get; set; }

    public string FileName { get; set; } = string.Empty;

    public string FilePath { get; set; } = string.Empty;

    public string? ThumbnailPath { get; set; }

    public string FileType { get; set; } = string.Empty; // image/jpeg, application/pdf

    public long FileSizeBytes { get; set; }

    public DateTime CreatedAt { get; set; }

    // Navigation
    public Receipt? Receipt { get; set; }
}