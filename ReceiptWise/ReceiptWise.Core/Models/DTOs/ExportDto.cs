namespace ReceiptWise.Core.Models.DTOs;

/// <summary>
/// DTO for CSV export
/// </summary>
public class ExportDto
{
    public string Date { get; set; } = string.Empty;
    public string Merchant { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public decimal Total { get; set; }
    public decimal Tax { get; set; }
    public string Currency { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
}