namespace ReceiptWise.Core.Models.Domain;

/// <summary>
/// Individual line item on a receipt
/// </summary>
public class ReceiptItem
{
    public int Id { get; set; }

    public int ReceiptId { get; set; }

    public string Description { get; set; } = string.Empty;

    public int Quantity { get; set; } = 1;

    public decimal UnitPrice { get; set; }

    public decimal TotalPrice { get; set; }

    // Navigation
    public Receipt? Receipt { get; set; }
}