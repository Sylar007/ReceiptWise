namespace ReceiptWise.Tests.Unit.Data;

using FluentAssertions;
using ReceiptWise.Data.Seed;
using Xunit;

public class DatabaseTests : TestBase
{
    [Fact]
    public async Task Database_Should_Initialize_Successfully()
    {
        // Assert
        var tableExists = await Database.TableExistsAsync("Receipts");
        tableExists.Should().BeTrue();
    }

    [Fact]
    public async Task Database_Should_Create_All_Required_Tables()
    {
        // Act & Assert
        (await Database.TableExistsAsync("Receipts")).Should().BeTrue();
        (await Database.TableExistsAsync("ReceiptItems")).Should().BeTrue();
        (await Database.TableExistsAsync("Attachments")).Should().BeTrue();
        (await Database.TableExistsAsync("Categories")).Should().BeTrue();
        (await Database.TableExistsAsync("WarrantyInfo")).Should().BeTrue();
    }

    [Fact]
    public async Task SampleDataSeeder_Should_Create_Sample_Receipts()
    {
        // Arrange
        var seeder = new SampleDataSeeder(Database);

        // Act
        await seeder.SeedCategoriesAsync();
        await seeder.SeedSampleReceiptsAsync(5);

        // Assert
        var count = await seeder.GetReceiptCountAsync();
        count.Should().Be(5);
    }

    [Fact]
    public async Task SampleDataSeeder_Should_Calculate_Total_Spent()
    {
        // Arrange
        var seeder = new SampleDataSeeder(Database);

        // Act
        await seeder.SeedCategoriesAsync();
        await seeder.SeedSampleReceiptsAsync(10);
        var totalSpent = await seeder.GetTotalSpentAsync();

        // Assert
        totalSpent.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task SampleDataSeeder_Should_Not_Duplicate_Data()
    {
        // Arrange
        var seeder = new SampleDataSeeder(Database);

        // Act
        await seeder.SeedCategoriesAsync();
        await seeder.SeedSampleReceiptsAsync(5);
        await seeder.SeedSampleReceiptsAsync(5); // Call again

        // Assert
        var count = await seeder.GetReceiptCountAsync();
        count.Should().Be(5); // Should not duplicate
    }

    [Fact]
    public async Task Database_Should_Support_Version_Management()
    {
        // Act
        await Database.SetDatabaseVersionAsync(1);
        var version = await Database.GetDatabaseVersionAsync();

        // Assert
        version.Should().Be(1);
    }

    [Fact]
    public async Task ClearAllDataAsync_Should_Remove_All_Records()
    {
        // Arrange
        var seeder = new SampleDataSeeder(Database);
        await seeder.SeedCategoriesAsync();
        await seeder.SeedSampleReceiptsAsync(10);

        // Act
        await Database.ClearAllDataAsync();
        var count = await seeder.GetReceiptCountAsync();

        // Assert
        count.Should().Be(0);
    }
}