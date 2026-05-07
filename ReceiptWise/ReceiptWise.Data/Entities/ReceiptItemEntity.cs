namespace ReceiptWise.Data.Entities;

using SQLite;

/// <summary>
/// SQLite entity for ReceiptItems table
/// Note: Using double instead of decimal for SQLite compatibility on Android
/// </summary>
[Table("ReceiptItems")]
public class ReceiptItemEntity
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    [Indexed]
    public int ReceiptId { get; set; }

    [MaxLength(500)]
    public string Description { get; set; } = string.Empty;

    public int Quantity { get; set; }

    // Changed from decimal to double for SQLite Android compatibility
    public double UnitPrice { get; set; }

    public double TotalPrice { get; set; }
}