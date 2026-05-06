namespace ReceiptWise.Tests.Unit.Repositories;

using FluentAssertions;
using ReceiptWise.Core.Enums;
using ReceiptWise.Data.Repositories;
using Xunit;

public class CategoryRepositoryTests : TestBase
{
    private readonly CategoryRepository _repository;

    public CategoryRepositoryTests()
    {
        _repository = new CategoryRepository(Database);
    }

    [Fact]
    public async Task InitializeDefaultCategoriesAsync_Should_Seed_All_Categories()
    {
        // Act
        await _repository.InitializeDefaultCategoriesAsync();
        var categories = await _repository.GetAllAsync();

        // Assert
        categories.Should().HaveCount(12);
        categories.Should().Contain(c => c.CategoryType == ReceiptCategory.Groceries);
        categories.Should().Contain(c => c.CategoryType == ReceiptCategory.Dining);
        categories.Should().Contain(c => c.CategoryType == ReceiptCategory.Technology);
    }

    [Fact]
    public async Task InitializeDefaultCategoriesAsync_Should_Not_Duplicate_If_Called_Twice()
    {
        // Act
        await _repository.InitializeDefaultCategoriesAsync();
        await _repository.InitializeDefaultCategoriesAsync();
        var categories = await _repository.GetAllAsync();

        // Assert
        categories.Should().HaveCount(12);
    }

    [Fact]
    public async Task GetAllAsync_Should_Return_Categories_With_Metadata()
    {
        // Arrange
        await _repository.InitializeDefaultCategoriesAsync();

        // Act
        var categories = await _repository.GetAllAsync();

        // Assert
        var groceries = categories.First(c => c.CategoryType == ReceiptCategory.Groceries);
        groceries.DisplayName.Should().Be("Groceries");
        groceries.Icon.Should().Be("🛒");
        groceries.Color.Should().Be("#4CAF50");
    }
}