namespace ReceiptWise.Core.Models.Domain;

/// <summary>
/// Warranty information for products
/// </summary>
public class WarrantyInfo
{
    public int Id { get; set; }

    public int ReceiptId { get; set; }

    public DateTime PurchaseDate { get; set; }

    public DateTime WarrantyEndDate { get; set; }

    public int WarrantyMonths { get; set; }

    public string? ProductName { get; set; }

    public string? WarrantyTerms { get; set; }

    public bool NotificationEnabled { get; set; } = true;

    // Navigation
    public Receipt? Receipt { get; set; }
}