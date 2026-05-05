namespace ReceiptWise.Data.Entities;

using SQLite;
using ReceiptWise.Core.Enums;

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

    public decimal Total { get; set; }

    public decimal Tax { get; set; }

    public decimal Subtotal { get; set; }

    public int Currency { get; set; } // Stored as int (enum)

    public int Category { get; set; } // Stored as int (enum)

    public int ExtractionStatus { get; set; } // Stored as int (enum)

    [MaxLength(1000)]
    public string? Notes { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? ModifiedAt { get; set; }

    // Ignored navigation properties (loaded separately)
    [Ignore]
    public List<ReceiptItemEntity> Items { get; set; } = new();

    [Ignore]
    public AttachmentEntity? Attachment { get; set; }

    [Ignore]
    public WarrantyInfoEntity? Warranty { get; set; }
}