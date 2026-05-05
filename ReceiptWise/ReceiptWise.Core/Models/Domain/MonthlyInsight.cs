namespace ReceiptWise.Core.Models.Domain;

using ReceiptWise.Core.Enums;

/// <summary>
/// Monthly spending insights
/// </summary>
public class MonthlyInsight
{
    public int Year { get; set; }

    public int Month { get; set; }

    public decimal TotalSpent { get; set; }

    public int ReceiptCount { get; set; }

    public Dictionary<ReceiptCategory, decimal> CategoryBreakdown { get; set; } = new();

    public ReceiptCategory TopCategory { get; set; }

    public decimal AverageReceiptAmount { get; set; }
}