namespace ReceiptWise.App.ViewModels;

using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;
using ReceiptWise.Services.Business;
using ReceiptWise.Core.Enums;

/// <summary>
/// ViewModel for Insights page with charts and analytics
/// </summary>
public partial class InsightsViewModel : BaseViewModel
{
    private readonly InsightsService _insightsService;

    [ObservableProperty]
    private decimal _currentMonthTotal;

    [ObservableProperty]
    private int _currentMonthReceiptCount;

    [ObservableProperty]
    private decimal _averageReceiptAmount;

    [ObservableProperty]
    private string _topCategory = string.Empty;

    [ObservableProperty]
    private decimal _lastMonthTotal;

    [ObservableProperty]
    private decimal _monthOverMonthChange;

    [ObservableProperty]
    private bool _isIncreaseFromLastMonth;

    [ObservableProperty]
    private string _selectedPeriod = "This Month";

    [ObservableProperty]
    private ObservableCollection<ISeries> _categoryChartSeries = new();

    [ObservableProperty]
    private ObservableCollection<ISeries> _trendChartSeries = new();

    [ObservableProperty]
    private ObservableCollection<Axis> _trendChartXAxes = new();

    [ObservableProperty]
    private ObservableCollection<MerchantInsight> _topMerchants = new();

    [ObservableProperty]
    private ObservableCollection<CategoryInsight> _categoryInsights = new();

    [ObservableProperty]
    private decimal _dailyAverage;

    public InsightsViewModel(InsightsService insightsService)
    {
        _insightsService = insightsService;
        Title = "Insights";
    }

    [RelayCommand]
    private async Task LoadInsightsAsync()
    {
        if (IsBusy)
            return;

        try
        {
            IsBusy = true;
            ClearError();

            await LoadCurrentMonthInsightsAsync();
            await LoadCategoryBreakdownAsync();
            await LoadMonthlyTrendAsync();
            await LoadTopMerchantsAsync();
        }
        catch (Exception ex)
        {
            SetError($"Failed to load insights: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task LoadCurrentMonthInsightsAsync()
    {
        var currentMonth = await _insightsService.GetCurrentMonthInsightAsync();

        CurrentMonthTotal = currentMonth.TotalSpent;
        CurrentMonthReceiptCount = currentMonth.ReceiptCount;
        AverageReceiptAmount = currentMonth.AverageReceiptAmount;
        TopCategory = currentMonth.TopCategory.ToString();

        // Compare with last month
        var lastMonth = await _insightsService.GetMonthlyInsightAsync(
            DateTime.Now.AddMonths(-1).Year,
            DateTime.Now.AddMonths(-1).Month);

        LastMonthTotal = lastMonth.TotalSpent;
        MonthOverMonthChange = CurrentMonthTotal - LastMonthTotal;
        IsIncreaseFromLastMonth = MonthOverMonthChange > 0;

        // Daily average for current month
        var startOfMonth = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
        var dailyInsight = await _insightsService.GetDailyAverageAsync(
            startOfMonth,
            DateTime.Now);

        DailyAverage = dailyInsight.AveragePerDay;
    }

    private async Task LoadCategoryBreakdownAsync()
    {
        var startOfMonth = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
        var breakdown = await _insightsService.GetCategoryBreakdownAsync(
            startOfMonth,
            DateTime.Now);

        // Update CategoryInsights collection for list view
        CategoryInsights.Clear();
        foreach (var kvp in breakdown.OrderByDescending(x => x.Value.TotalSpent))
        {
            CategoryInsights.Add(kvp.Value);
        }

        // Create pie chart series
        var pieSeries = new ObservableCollection<ISeries>();

        foreach (var kvp in breakdown.OrderByDescending(x => x.Value.TotalSpent))
        {
            var category = kvp.Key;
            var insight = kvp.Value;

            pieSeries.Add(new PieSeries<decimal>
            {
                Name = category.ToString(),
                Values = new[] { insight.TotalSpent },
                DataLabelsPaint = new SolidColorPaint(SKColors.White),
                DataLabelsSize = 14,
                DataLabelsPosition = LiveChartsCore.Measure.PolarLabelsPosition.Middle,
                DataLabelsFormatter = point => $"${point.PrimaryValue:F0}"
            });
        }

        CategoryChartSeries = pieSeries;
    }

    private async Task LoadMonthlyTrendAsync()
    {
        var trends = await _insightsService.GetMonthlyTrendAsync(6);

        var values = trends.Select(t => (double)t.TotalSpent).ToArray();
        var labels = trends.Select(t => $"{GetMonthName(t.Month)} {t.Year}").ToArray();

        TrendChartSeries = new ObservableCollection<ISeries>
        {
            new LineSeries<double>
            {
                Name = "Monthly Spending",
                Values = values,
                Fill = null,
                GeometrySize = 10,
                LineSmoothness = 0.5,
                Stroke = new SolidColorPaint(SKColor.Parse("#512BD4")) { StrokeThickness = 3 }
            }
        };

        TrendChartXAxes = new ObservableCollection<Axis>
        {
            new Axis
            {
                Labels = labels,
                LabelsRotation = 45,
                TextSize = 12
            }
        };
    }

    private async Task LoadTopMerchantsAsync()
    {
        var startOfMonth = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
        var merchants = await _insightsService.GetTopMerchantsAsync(
            startOfMonth,
            DateTime.Now,
            topCount: 5);

        TopMerchants.Clear();
        foreach (var merchant in merchants)
        {
            TopMerchants.Add(merchant);
        }
    }

    [RelayCommand]
    private async Task ChangePeriodAsync(string period)
    {
        SelectedPeriod = period;

        DateTime startDate;
        DateTime endDate = DateTime.Now;

        switch (period)
        {
            case "This Week":
                startDate = DateTime.Now.AddDays(-(int)DateTime.Now.DayOfWeek);
                break;
            case "This Month":
                startDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
                break;
            case "Last 30 Days":
                startDate = DateTime.Now.AddDays(-30);
                break;
            case "Last 90 Days":
                startDate = DateTime.Now.AddDays(-90);
                break;
            case "This Year":
                startDate = new DateTime(DateTime.Now.Year, 1, 1);
                break;
            default:
                startDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
                break;
        }

        await LoadPeriodDataAsync(startDate, endDate);
    }

    private async Task LoadPeriodDataAsync(DateTime startDate, DateTime endDate)
    {
        try
        {
            IsBusy = true;

            var breakdown = await _insightsService.GetCategoryBreakdownAsync(startDate, endDate);

            // Update charts
            var pieSeries = new ObservableCollection<ISeries>();
            CategoryInsights.Clear();

            foreach (var kvp in breakdown.OrderByDescending(x => x.Value.TotalSpent))
            {
                CategoryInsights.Add(kvp.Value);

                pieSeries.Add(new PieSeries<decimal>
                {
                    Name = kvp.Key.ToString(),
                    Values = new[] { kvp.Value.TotalSpent },
                    DataLabelsPaint = new SolidColorPaint(SKColors.White),
                    DataLabelsSize = 14,
                    DataLabelsPosition = LiveChartsCore.Measure.PolarLabelsPosition.Middle,
                    DataLabelsFormatter = point => $"${point.PrimaryValue:F0}"
                });
            }

            CategoryChartSeries = pieSeries;

            // Update top merchants
            var merchants = await _insightsService.GetTopMerchantsAsync(startDate, endDate, 5);
            TopMerchants.Clear();
            foreach (var merchant in merchants)
            {
                TopMerchants.Add(merchant);
            }
        }
        catch (Exception ex)
        {
            SetError($"Failed to load period data: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task ExportInsightsAsync()
    {
        try
        {
            var csv = GenerateInsightsCsv();
            var fileName = $"Insights_{DateTime.Now:yyyyMMdd}.csv";
            var filePath = Path.Combine(FileSystem.CacheDirectory, fileName);

            await File.WriteAllTextAsync(filePath, csv);

            await Share.RequestAsync(new ShareFileRequest
            {
                Title = "Export Insights",
                File = new ShareFile(filePath)
            });

            await Shell.Current.DisplayAlert("Success", "Insights exported successfully", "OK");
        }
        catch (Exception ex)
        {
            SetError($"Failed to export insights: {ex.Message}");
        }
    }

    private string GenerateInsightsCsv()
    {
        var sb = new System.Text.StringBuilder();

        sb.AppendLine("ReceiptWise - Spending Insights");
        sb.AppendLine($"Generated: {DateTime.Now:yyyy-MM-dd HH:mm}");
        sb.AppendLine();

        sb.AppendLine("Summary");
        sb.AppendLine($"Period,{SelectedPeriod}");
        sb.AppendLine($"Total Spent,${CurrentMonthTotal:F2}");
        sb.AppendLine($"Receipt Count,{CurrentMonthReceiptCount}");
        sb.AppendLine($"Average Receipt,${AverageReceiptAmount:F2}");
        sb.AppendLine($"Daily Average,${DailyAverage:F2}");
        sb.AppendLine();

        sb.AppendLine("Category Breakdown");
        sb.AppendLine("Category,Amount,Receipts,Percentage");
        foreach (var insight in CategoryInsights)
        {
            sb.AppendLine($"{insight.Category},${insight.TotalSpent:F2},{insight.ReceiptCount},{insight.Percentage:F1}%");
        }
        sb.AppendLine();

        sb.AppendLine("Top Merchants");
        sb.AppendLine("Merchant,Amount,Receipts,Category");
        foreach (var merchant in TopMerchants)
        {
            sb.AppendLine($"\"{merchant.MerchantName}\",${merchant.TotalSpent:F2},{merchant.ReceiptCount},{merchant.Category}");
        }

        return sb.ToString();
    }

    private string GetMonthName(int month)
    {
        return month switch
        {
            1 => "Jan",
            2 => "Feb",
            3 => "Mar",
            4 => "Apr",
            5 => "May",
            6 => "Jun",
            7 => "Jul",
            8 => "Aug",
            9 => "Sep",
            10 => "Oct",
            11 => "Nov",
            12 => "Dec",
            _ => ""
        };
    }
}