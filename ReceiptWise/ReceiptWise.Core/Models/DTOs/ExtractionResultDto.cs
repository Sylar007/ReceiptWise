namespace ReceiptWise.Core.Models.DTOs;

using ReceiptWise.Core.Enums;

/// <summary>
/// Result from Azure Document Intelligence extraction
/// </summary>
public class ExtractionResultDto
{
    public bool Success { get; set; }

    public string? ErrorMessage { get; set; }

    public string MerchantName { get; set; } = string.Empty;

    public DateTime? TransactionDate { get; set; }

    public decimal Total { get; set; }

    public decimal Tax { get; set; }

    public decimal Subtotal { get; set; }

    public CurrencyCode Currency { get; set; } = CurrencyCode.USD;

    public List<ReceiptItemDto> Items { get; set; } = new();

    public float Confidence { get; set; }
}

public class ReceiptItemDto
{
    public string Description { get; set; } = string.Empty;

    public int Quantity { get; set; } = 1;

    public decimal Price { get; set; }
}