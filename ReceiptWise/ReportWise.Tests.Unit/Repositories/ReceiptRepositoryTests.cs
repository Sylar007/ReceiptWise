namespace ReceiptWise.Tests.Unit.Repositories;

using FluentAssertions;
using ReceiptWise.Core.Enums;
using ReceiptWise.Core.Models.Domain;
using ReceiptWise.Data.Repositories;
using Xunit;

public class ReceiptRepositoryTests : TestBase
{
    private readonly ReceiptRepository _repository;

    public ReceiptRepositoryTests()
    {
        _repository = new ReceiptRepository(Database);
    }

    [Fact]
    public async Task AddAsync_Should_Insert_Receipt_And_Return_Id()
    {
        // Arrange
        var receipt = new Receipt
        {
            MerchantName = "Test Store",
            TransactionDate = DateTime.Now,
            Total = 99.99m,
            Tax = 8.75m,
            Subtotal = 91.24m,
            Currency = CurrencyCode.USD,
            Category = ReceiptCategory.Groceries,
            ExtractionStatus = ExtractionStatus.ManualEntry,
            Items = new List<ReceiptItem>
            {
                new ReceiptItem
                {
                    Description = "Test Item",
                    Quantity = 2,
                    UnitPrice = 45.62m,
                    TotalPrice = 91.24m
                }
            }
        };

        // Act
        var receiptId = await _repository.AddAsync(receipt);

        // Assert
        receiptId.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task GetByIdAsync_Should_Return_Receipt_With_Items()
    {
        // Arrange
        var receipt = CreateTestReceipt();
        var receiptId = await _repository.AddAsync(receipt);

        // Act
        var result = await _repository.GetByIdAsync(receiptId);

        // Assert
        result.Should().NotBeNull();
        result!.MerchantName.Should().Be("Test Store");
        result.Total.Should().Be(99.99m);
        result.Items.Should().HaveCount(2);
        result.Items.First().Description.Should().Be("Item 1");
    }

    [Fact]
    public async Task GetAllAsync_Should_Return_All_Receipts()
    {
        // Arrange
        await _repository.AddAsync(CreateTestReceipt("Store A"));
        await _repository.AddAsync(CreateTestReceipt("Store B"));
        await _repository.AddAsync(CreateTestReceipt("Store C"));

        // Act
        var results = await _repository.GetAllAsync();

        // Assert
        results.Should().HaveCount(3);
    }

    [Fact]
    public async Task SearchAsync_Should_Filter_By_MerchantName()
    {
        // Arrange
        await _repository.AddAsync(CreateTestReceipt("Walmart"));
        await _repository.AddAsync(CreateTestReceipt("Target"));
        await _repository.AddAsync(CreateTestReceipt("Walmart Supercenter"));

        // Act
        var results = await _repository.SearchAsync(searchTerm: "Walmart");

        // Assert
        results.Should().HaveCount(2);
        results.Should().AllSatisfy(r => r.MerchantName.Should().Contain("Walmart"));
    }

    [Fact]
    public async Task SearchAsync_Should_Filter_By_Category()
    {
        // Arrange
        var groceries = CreateTestReceipt("Store A");
        groceries.Category = ReceiptCategory.Groceries;
        await _repository.AddAsync(groceries);

        var dining = CreateTestReceipt("Store B");
        dining.Category = ReceiptCategory.Dining;
        await _repository.AddAsync(dining);

        var groceries2 = CreateTestReceipt("Store C");
        groceries2.Category = ReceiptCategory.Groceries;
        await _repository.AddAsync(groceries2);

        // Act
        var results = await _repository.SearchAsync(category: ReceiptCategory.Groceries);

        // Assert
        results.Should().HaveCount(2);
        results.Should().AllSatisfy(r => r.Category.Should().Be(ReceiptCategory.Groceries));
    }

    [Fact]
    public async Task SearchAsync_Should_Filter_By_Amount_Range()
    {
        // Arrange
        var cheap = CreateTestReceipt("Store A");
        cheap.Total = 25.00m;
        await _repository.AddAsync(cheap);

        var medium = CreateTestReceipt("Store B");
        medium.Total = 75.00m;
        await _repository.AddAsync(medium);

        var expensive = CreateTestReceipt("Store C");
        expensive.Total = 150.00m;
        await _repository.AddAsync(expensive);

        // Act
        var results = await _repository.SearchAsync(minAmount: 50m, maxAmount: 100m);

        // Assert
        results.Should().HaveCount(1);
        results.First().Total.Should().Be(75.00m);
    }

    [Fact]
    public async Task SearchAsync_Should_Filter_By_Date_Range()
    {
        // Arrange
        var old = CreateTestReceipt("Store A");
        old.TransactionDate = DateTime.Now.AddDays(-60);
        await _repository.AddAsync(old);

        var recent = CreateTestReceipt("Store B");
        recent.TransactionDate = DateTime.Now.AddDays(-15);
        await _repository.AddAsync(recent);

        var veryRecent = CreateTestReceipt("Store C");
        veryRecent.TransactionDate = DateTime.Now.AddDays(-5);
        await _repository.AddAsync(veryRecent);

        // Act
        var results = await _repository.SearchAsync(
            startDate: DateTime.Now.AddDays(-30),
            endDate: DateTime.Now);

        // Assert
        results.Should().HaveCount(2);
    }

    [Fact]
    public async Task UpdateAsync_Should_Modify_Existing_Receipt()
    {
        // Arrange
        var receipt = CreateTestReceipt();
        var receiptId = await _repository.AddAsync(receipt);

        var retrievedReceipt = await _repository.GetByIdAsync(receiptId);
        retrievedReceipt!.MerchantName = "Updated Store";
        retrievedReceipt.Total = 150.00m;
        retrievedReceipt.Items.Add(new ReceiptItem
        {
            Description = "New Item",
            Quantity = 1,
            UnitPrice = 50.00m,
            TotalPrice = 50.00m
        });

        // Act
        await _repository.UpdateAsync(retrievedReceipt);
        var updated = await _repository.GetByIdAsync(receiptId);

        // Assert
        updated.Should().NotBeNull();
        updated!.MerchantName.Should().Be("Updated Store");
        updated.Total.Should().Be(150.00m);
        updated.Items.Should().HaveCount(3);
    }

    [Fact]
    public async Task DeleteAsync_Should_Remove_Receipt_And_RelatedData()
    {
        // Arrange
        var receipt = CreateTestReceipt();
        var receiptId = await _repository.AddAsync(receipt);

        // Act
        await _repository.DeleteAsync(receiptId);
        var result = await _repository.GetByIdAsync(receiptId);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetCountAsync_Should_Return_Total_Receipt_Count()
    {
        // Arrange
        await _repository.AddAsync(CreateTestReceipt("Store A"));
        await _repository.AddAsync(CreateTestReceipt("Store B"));
        await _repository.AddAsync(CreateTestReceipt("Store C"));

        // Act
        var count = await _repository.GetCountAsync();

        // Assert
        count.Should().Be(3);
    }

    [Fact]
    public async Task GetByDateRangeAsync_Should_Return_Receipts_In_Range()
    {
        // Arrange
        var oldReceipt = CreateTestReceipt("Old Store");
        oldReceipt.TransactionDate = new DateTime(2024, 1, 15);
        await _repository.AddAsync(oldReceipt);

        var recentReceipt = CreateTestReceipt("Recent Store");
        recentReceipt.TransactionDate = new DateTime(2024, 5, 15);
        await _repository.AddAsync(recentReceipt);

        // Act
        var results = await _repository.GetByDateRangeAsync(
            new DateTime(2024, 5, 1),
            new DateTime(2024, 5, 31));

        // Assert
        results.Should().HaveCount(1);
        results.First().MerchantName.Should().Be("Recent Store");
    }

    // Helper method to create test receipts
    private Receipt CreateTestReceipt(string merchantName = "Test Store")
    {
        return new Receipt
        {
            MerchantName = merchantName,
            TransactionDate = DateTime.Now,
            Total = 99.99m,
            Tax = 8.75m,
            Subtotal = 91.24m,
            Currency = CurrencyCode.USD,
            Category = ReceiptCategory.Groceries,
            ExtractionStatus = ExtractionStatus.ManualEntry,
            Items = new List<ReceiptItem>
            {
                new ReceiptItem
                {
                    Description = "Item 1",
                    Quantity = 2,
                    UnitPrice = 25.00m,
                    TotalPrice = 50.00m
                },
                new ReceiptItem
                {
                    Description = "Item 2",
                    Quantity = 1,
                    UnitPrice = 41.24m,
                    TotalPrice = 41.24m
                }
            }
        };
    }
}