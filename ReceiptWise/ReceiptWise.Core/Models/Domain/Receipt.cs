namespace ReceiptWise.Core.Models.Domain;

using ReceiptWise.Core.Enums;
using System.Net.Mail;

/// <summary>
/// Core receipt domain model
/// </summary>
public class Receipt
{
    public int Id { get; set; }

    public string MerchantName { get; set; } = string.Empty;

    public DateTime TransactionDate { get; set; }

    public decimal Total { get; set; }

    public decimal Tax { get; set; }

    public decimal Subtotal { get; set; }

    public CurrencyCode Currency { get; set; } = CurrencyCode.USD;

    public ReceiptCategory Category { get; set; }

    public ExtractionStatus ExtractionStatus { get; set; }

    public string? Notes { get; set; }

    public bool IsSelected { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? ModifiedAt { get; set; }

    // Navigation properties
    public List<ReceiptItem> Items { get; set; } = new();

    public Attachment? Attachment { get; set; }

    public WarrantyInfo? Warranty { get; set; }
}