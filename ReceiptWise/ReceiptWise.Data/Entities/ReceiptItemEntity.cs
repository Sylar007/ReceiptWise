namespace ReceiptWise.Data.Entities;

using SQLite;

/// <summary>
/// SQLite entity for ReceiptItems table
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

    public decimal UnitPrice { get; set; }

    public decimal TotalPrice { get; set; }
}