namespace ReceiptWise.Services.Business;

using Microsoft.Extensions.Logging;
using ReceiptWise.Core.Interfaces.Repositories;
using ReceiptWise.Core.Models.Domain;
using ReceiptWise.Core.Enums;

/// <summary>
/// Service for calculating spending insights and analytics
/// </summary>
public class InsightsService
{
    private readonly IReceiptRepository _receiptRepository;
    private readonly ILogger<InsightsService>? _logger;

    public InsightsService(
        IReceiptRepository receiptRepository,
        ILogger<InsightsService>? logger = null)
    {
        _receiptRepository = receiptRepository;
        _logger = logger;
    }

    /// <summary>
    /// Get monthly insights for a specific month/year
    /// </summary>
    public async Task<MonthlyInsight> GetMonthlyInsightAsync(
        int year,
        int month,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var startDate = new DateTime(year, month, 1);
            var endDate = startDate.AddMonths(1).AddDays(-1);

            var receipts = await _receiptRepository.GetByDateRangeAsync(
                startDate,
                endDate,
                cancellationToken);

            return CalculateMonthlyInsight(receipts.ToList(), year, month);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to get monthly insight for {Year}-{Month}", year, month);
            throw;
        }
    }

    /// <summary>
    /// Get insights for the current month
    /// </summary>
    public async Task<MonthlyInsight> GetCurrentMonthInsightAsync(
        CancellationToken cancellationToken = default)
    {
        var now = DateTime.Now;
        return await GetMonthlyInsightAsync(now.Year, now.Month, cancellationToken);
    }

    /// <summary>
    /// Get insights for multiple months (trend analysis)
    /// </summary>
    public async Task<List<MonthlyInsight>> GetMonthlyTrendAsync(
        int numberOfMonths = 6,
        CancellationToken cancellationToken = default)
    {
        var insights = new List<MonthlyInsight>();
        var currentDate = DateTime.Now;

        for (int i = numberOfMonths - 1; i >= 0; i--)
        {
            var targetDate = currentDate.AddMonths(-i);
            var insight = await GetMonthlyInsightAsync(
                targetDate.Year,
                targetDate.Month,
                cancellationToken);

            insights.Add(insight);
        }

        return insights;
    }

    /// <summary>
    /// Get category breakdown for a date range
    /// </summary>
    public async Task<Dictionary<ReceiptCategory, CategoryInsight>> GetCategoryBreakdownAsync(
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken = default)
    {
        var receipts = await _receiptRepository.GetByDateRangeAsync(
            startDate,
            endDate,
            cancellationToken);

        var breakdown = receipts
            .GroupBy(r => r.Category)
            .ToDictionary(
                g => g.Key,
                g => new CategoryInsight
                {
                    Category = g.Key,
                    TotalSpent = g.Sum(r => r.Total),
                    ReceiptCount = g.Count(),
                    AverageAmount = g.Average(r => r.Total),
                    Percentage = 0 // Will be calculated after
                });

        // Calculate percentages
        var totalSpent = breakdown.Values.Sum(c => c.TotalSpent);
        if (totalSpent > 0)
        {
            foreach (var insight in breakdown.Values)
            {
                insight.Percentage = (float)(insight.TotalSpent / totalSpent * 100);
            }
        }

        return breakdown;
    }

    /// <summary>
    /// Get top merchants by spending
    /// </summary>
    public async Task<List<MerchantInsight>> GetTopMerchantsAsync(
        DateTime startDate,
        DateTime endDate,
        int topCount = 10,
        CancellationToken cancellationToken = default)
    {
        var receipts = await _receiptRepository.GetByDateRangeAsync(
            startDate,
            endDate,
            cancellationToken);

        var merchantInsights = receipts
            .GroupBy(r => r.MerchantName)
            .Select(g => new MerchantInsight
            {
                MerchantName = g.Key,
                TotalSpent = g.Sum(r => r.Total),
                ReceiptCount = g.Count(),
                AverageAmount = g.Average(r => r.Total),
                Category = g.First().Category
            })
            .OrderByDescending(m => m.TotalSpent)
            .Take(topCount)
            .ToList();

        return merchantInsights;
    }

    /// <summary>
    /// Get spending comparison between two periods
    /// </summary>
    public async Task<SpendingComparison> ComparePeriodsAsync(
        DateTime period1Start,
        DateTime period1End,
        DateTime period2Start,
        DateTime period2End,
        CancellationToken cancellationToken = default)
    {
        var receipts1 = await _receiptRepository.GetByDateRangeAsync(
            period1Start,
            period1End,
            cancellationToken);

        var receipts2 = await _receiptRepository.GetByDateRangeAsync(
            period2Start,
            period2End,
            cancellationToken);

        var total1 = receipts1.Sum(r => r.Total);
        var total2 = receipts2.Sum(r => r.Total);

        return new SpendingComparison
        {
            Period1Label = $"{period1Start:MMM yyyy}",
            Period2Label = $"{period2Start:MMM yyyy}",
            Period1Total = total1,
            Period2Total = total2,
            Difference = total2 - total1,
            PercentageChange = total1 > 0 ? (float)((total2 - total1) / total1 * 100) : 0,
            IsIncrease = total2 > total1
        };
    }

    /// <summary>
    /// Calculate daily average spending
    /// </summary>
    public async Task<DailyAverageInsight> GetDailyAverageAsync(
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken = default)
    {
        var receipts = await _receiptRepository.GetByDateRangeAsync(
            startDate,
            endDate,
            cancellationToken);

        var totalDays = (endDate - startDate).Days + 1;
        var totalSpent = receipts.Sum(r => r.Total);
        var daysWithReceipts = receipts.Select(r => r.TransactionDate.Date).Distinct().Count();

        return new DailyAverageInsight
        {
            TotalDays = totalDays,
            DaysWithReceipts = daysWithReceipts,
            TotalSpent = totalSpent,
            AveragePerDay = totalDays > 0 ? totalSpent / totalDays : 0,
            AveragePerReceiptDay = daysWithReceipts > 0 ? totalSpent / daysWithReceipts : 0
        };
    }

    /// <summary>
    /// Calculate monthly insight from receipts
    /// </summary>
    private MonthlyInsight CalculateMonthlyInsight(List<Receipt> receipts, int year, int month)
    {
        var totalSpent = receipts.Sum(r => r.Total);

        var categoryBreakdown = receipts
            .GroupBy(r => r.Category)
            .ToDictionary(
                g => g.Key,
                g => g.Sum(r => r.Total));

        var topCategory = categoryBreakdown.Any()
            ? categoryBreakdown.OrderByDescending(kvp => kvp.Value).First().Key
            : ReceiptCategory.Other;

        return new MonthlyInsight
        {
            Year = year,
            Month = month,
            TotalSpent = totalSpent,
            ReceiptCount = receipts.Count,
            CategoryBreakdown = categoryBreakdown,
            TopCategory = topCategory,
            AverageReceiptAmount = receipts.Any() ? receipts.Average(r => r.Total) : 0
        };
    }
}

/// <summary>
/// Insights for a specific category
/// </summary>
public class CategoryInsight
{
    public ReceiptCategory Category { get; set; }
    public decimal TotalSpent { get; set; }
    public int ReceiptCount { get; set; }
    public decimal AverageAmount { get; set; }
    public float Percentage { get; set; }
}

/// <summary>
/// Insights for a specific merchant
/// </summary>
public class MerchantInsight
{
    public string MerchantName { get; set; } = string.Empty;
    public decimal TotalSpent { get; set; }
    public int ReceiptCount { get; set; }
    public decimal AverageAmount { get; set; }
    public ReceiptCategory Category { get; set; }
}

/// <summary>
/// Comparison between two spending periods
/// </summary>
public class SpendingComparison
{
    public string Period1Label { get; set; } = string.Empty;
    public string Period2Label { get; set; } = string.Empty;
    public decimal Period1Total { get; set; }
    public decimal Period2Total { get; set; }
    public decimal Difference { get; set; }
    public float PercentageChange { get; set; }
    public bool IsIncrease { get; set; }
}

/// <summary>
/// Daily average spending insights
/// </summary>
public class DailyAverageInsight
{
    public int TotalDays { get; set; }
    public int DaysWithReceipts { get; set; }
    public decimal TotalSpent { get; set; }
    public decimal AveragePerDay { get; set; }
    public decimal AveragePerReceiptDay { get; set; }
}