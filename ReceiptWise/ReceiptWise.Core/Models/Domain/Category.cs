namespace ReceiptWise.Core.Models.Domain;

using ReceiptWise.Core.Enums;

/// <summary>
/// Category metadata (for future expansion)
/// </summary>
public class Category
{
    public int Id { get; set; }

    public ReceiptCategory CategoryType { get; set; }

    public string DisplayName { get; set; } = string.Empty;

    public string? Icon { get; set; }

    public string? Color { get; set; }
}