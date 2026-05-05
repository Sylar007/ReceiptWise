namespace ReceiptWise.Core.Models.DTOs;

using ReceiptWise.Core.Enums;

/// <summary>
/// Category suggestion result
/// </summary>
public class CategorySuggestionDto
{
    public ReceiptCategory SuggestedCategory { get; set; }

    public float Confidence { get; set; }

    public string Reasoning { get; set; } = string.Empty;
}