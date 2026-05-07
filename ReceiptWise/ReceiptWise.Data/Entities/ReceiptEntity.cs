namespace ReceiptWise.Data.Entities;

using SQLite;

/// <summary>
/// SQLite entity for Receipt table
/// </summary>
[Table("Receipts")]
public class ReceiptEntity
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    [MaxLength(500)]
    public string MerchantName { get; set; } = string.Empty;

    public DateTime TransactionDate { get; set; }

    public double Total { get; set; }

    public double Tax { get; set; }

    public double Subtotal { get; set; }

    public int Currency { get; set; }

    public int Category { get; set; }

    public int ExtractionStatus { get; set; }

    [MaxLength(1000)]
    public string? Notes { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? ModifiedAt { get; set; }

    // Navigation properties - marked with Ignore and nullable
    [Ignore]
    public List<ReceiptItemEntity>? Items { get; set; }

    [Ignore]
    public AttachmentEntity? Attachment { get; set; }

    [Ignore]
    public WarrantyInfoEntity? Warranty { get; set; }
}