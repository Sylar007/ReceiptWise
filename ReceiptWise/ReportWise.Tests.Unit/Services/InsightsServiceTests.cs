namespace ReceiptWise.Tests.Unit.Services;

using FluentAssertions;
using ReceiptWise.Core.Enums;
using ReceiptWise.Core.Models.Domain;
using ReceiptWise.Data.Repositories;
using ReceiptWise.Services.Business;
using Xunit;

public class InsightsServiceTests : TestBase
{
    private readonly InsightsService _service;
    private readonly ReceiptRepository _repository;

    public InsightsServiceTests()
    {
        _repository = new ReceiptRepository(Database);
        _service = new InsightsService(_repository);
    }

    [Fact]
    public async Task GetMonthlyInsightAsync_Should_Calculate_Correct_Totals()
    {
        // Arrange
        await SeedTestReceipts();

        // Act
        var insight = await _service.GetMonthlyInsightAsync(DateTime.Now.Year, DateTime.Now.Month);

        // Assert
        insight.Should().NotBeNull();
        insight.TotalSpent.Should().BeGreaterThan(0);
        insight.ReceiptCount.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task GetCategoryBreakdownAsync_Should_Group_By_Category()
    {
        // Arrange
        await SeedTestReceipts();
        var startOfMonth = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);

        // Act
        var breakdown = await _service.GetCategoryBreakdownAsync(startOfMonth, DateTime.Now);

        // Assert
        breakdown.Should().NotBeEmpty();
        breakdown.Values.Sum(c => c.Percentage).Should().BeApproximately(100f, 0.1f);
    }

    [Fact]
    public async Task GetTopMerchantsAsync_Should_Return_Top_Spenders()
    {
        // Arrange
        await SeedMerchantReceipts();
        var startOfMonth = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);

        // Act
        var merchants = await _service.GetTopMerchantsAsync(startOfMonth, DateTime.Now, 3);

        // Assert
        merchants.Should().HaveCountLessOrEqualTo(3);
        merchants.Should().BeInDescendingOrder(m => m.TotalSpent);
    }

    [Fact]
    public async Task GetMonthlyTrendAsync_Should_Return_Multiple_Months()
    {
        // Arrange
        await SeedHistoricalReceipts();

        // Act
        var trends = await _service.GetMonthlyTrendAsync(6);

        // Assert
        trends.Should().HaveCount(6);
        trends.Should().BeInAscendingOrder(t => new DateTime(t.Year, t.Month, 1));
    }

    private async Task SeedTestReceipts()
    {
        var receipts = new[]
        {
            CreateReceipt("Walmart", 125.50m, ReceiptCategory.Groceries),
            CreateReceipt("Starbucks", 24.30m, ReceiptCategory.Dining),
            CreateReceipt("Shell", 52.00m, ReceiptCategory.Transportation)
        };

        foreach (var receipt in receipts)
        {
            await _repository.AddAsync(receipt);
        }
    }

    private async Task SeedMerchantReceipts()
    {
        var receipts = new[]
        {
            CreateReceipt("Walmart", 125.50m, ReceiptCategory.Groceries),
            CreateReceipt("Walmart", 89.99m, ReceiptCategory.Groceries),
            CreateReceipt("Target", 150.00m, ReceiptCategory.Shopping),
            CreateReceipt("Starbucks", 24.30m, ReceiptCategory.Dining),
            CreateReceipt("Starbucks", 18.75m, ReceiptCategory.Dining)
        };

        foreach (var receipt in receipts)
        {
            await _repository.AddAsync(receipt);
        }
    }

    private async Task SeedHistoricalReceipts()
    {
        for (int i = 0; i < 6; i++)
        {
            var date = DateTime.Now.AddMonths(-i);
            var receipt = CreateReceipt("Test Store", 100m, ReceiptCategory.Other);
            receipt.TransactionDate = date;
            await _repository.AddAsync(receipt);
        }
    }

    private Receipt CreateReceipt(string merchant, decimal total, ReceiptCategory category)
    {
        return new Receipt
        {
            MerchantName = merchant,
            TransactionDate = DateTime.Now,
            Total = total,
            Tax = total * 0.0875m,
            Subtotal = total - (total * 0.0875m),
            Currency = CurrencyCode.USD,
            Category = category,
            ExtractionStatus = ExtractionStatus.ManualEntry
        };
    }
}