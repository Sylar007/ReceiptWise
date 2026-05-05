namespace ReceiptWise.Data.Entities;

using SQLite;

/// <summary>
/// SQLite entity for WarrantyInfo table
/// </summary>
[Table("WarrantyInfo")]
public class WarrantyInfoEntity
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    [Indexed, Unique]
    public int ReceiptId { get; set; }

    public DateTime PurchaseDate { get; set; }

    public DateTime WarrantyEndDate { get; set; }

    public int WarrantyMonths { get; set; }

    [MaxLength(500)]
    public string? ProductName { get; set; }

    [MaxLength(1000)]
    public string? WarrantyTerms { get; set; }

    public bool NotificationEnabled { get; set; }
}