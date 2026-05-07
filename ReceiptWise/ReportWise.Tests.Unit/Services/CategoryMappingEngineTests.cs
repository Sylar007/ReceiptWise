namespace ReceiptWise.Tests.Unit.Services;

using FluentAssertions;
using ReceiptWise.Core.Enums;
using ReceiptWise.Services.Business;
using Xunit;

public class CategoryMappingEngineTests
{
    private readonly CategoryMappingEngine _engine;

    public CategoryMappingEngineTests()
    {
        _engine = new CategoryMappingEngine();
    }

    [Theory]
    [InlineData("Walmart Supercenter", ReceiptCategory.Groceries)]
    [InlineData("Whole Foods Market", ReceiptCategory.Groceries)]
    [InlineData("Kroger", ReceiptCategory.Groceries)]
    [InlineData("Target", ReceiptCategory.Groceries)]
    public void SuggestCategory_Should_Match_Grocery_Stores(string merchant, ReceiptCategory expected)
    {
        // Act
        var result = _engine.SuggestCategory(merchant);

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("McDonald's", ReceiptCategory.Dining)]
    [InlineData("Starbucks Coffee", ReceiptCategory.Dining)]
    [InlineData("Chipotle Mexican Grill", ReceiptCategory.Dining)]
    [InlineData("Pizza Hut", ReceiptCategory.Dining)]
    public void SuggestCategory_Should_Match_Restaurants(string merchant, ReceiptCategory expected)
    {
        // Act
        var result = _engine.SuggestCategory(merchant);

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("Shell Gas Station", ReceiptCategory.Transportation)]
    [InlineData("Chevron", ReceiptCategory.Transportation)]
    [InlineData("Uber Trip", ReceiptCategory.Transportation)]
    [InlineData("BP Fuel", ReceiptCategory.Transportation)]
    public void SuggestCategory_Should_Match_Transportation(string merchant, ReceiptCategory expected)
    {
        // Act
        var result = _engine.SuggestCategory(merchant);

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("Best Buy", ReceiptCategory.Technology)]
    [InlineData("Apple Store", ReceiptCategory.Technology)]
    [InlineData("Microsoft Store", ReceiptCategory.Technology)]
    public void SuggestCategory_Should_Match_Technology(string merchant, ReceiptCategory expected)
    {
        // Act
        var result = _engine.SuggestCategory(merchant);

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("CVS Pharmacy", ReceiptCategory.Healthcare)]
    [InlineData("Walgreens", ReceiptCategory.Healthcare)]
    [InlineData("Medical Clinic", ReceiptCategory.Healthcare)]
    public void SuggestCategory_Should_Match_Healthcare(string merchant, ReceiptCategory expected)
    {
        // Act
        var result = _engine.SuggestCategory(merchant);

        // Assert
        result.Should().Be(expected);
    }

    [Fact]
    public void SuggestCategory_Should_Return_Null_For_Unknown_Merchant()
    {
        // Act
        var result = _engine.SuggestCategory("Random Unknown Store XYZ");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void SuggestCategory_Should_Handle_Case_Insensitive_Matching()
    {
        // Act
        var result1 = _engine.SuggestCategory("WALMART");
        var result2 = _engine.SuggestCategory("walmart");
        var result3 = _engine.SuggestCategory("WalMart");

        // Assert
        result1.Should().Be(ReceiptCategory.Groceries);
        result2.Should().Be(ReceiptCategory.Groceries);
        result3.Should().Be(ReceiptCategory.Groceries);
    }

    [Fact]
    public void SuggestCategory_Should_Handle_Partial_Matches()
    {
        // Act
        var result = _engine.SuggestCategory("Walmart Neighborhood Market #1234");

        // Assert
        result.Should().Be(ReceiptCategory.Groceries);
    }

    [Fact]
    public void SuggestCategory_Should_Match_By_Items_When_Merchant_Unknown()
    {
        // Arrange
        var items = new[] { "Milk", "Bread", "Eggs", "Cheese" };

        // Act
        var result = _engine.SuggestCategory("Unknown Store", items);

        // Assert
        result.Should().Be(ReceiptCategory.Groceries);
    }

    [Fact]
    public void GetMatchConfidence_Should_Return_High_For_Exact_Match()
    {
        // Act
        var confidence = _engine.GetMatchConfidence("Walmart", ReceiptCategory.Groceries);

        // Assert
        confidence.Should().BeGreaterThan(0.8f);
    }

    [Fact]
    public void GetMatchConfidence_Should_Return_Zero_For_Wrong_Category()
    {
        // Act
        var confidence = _engine.GetMatchConfidence("Walmart", ReceiptCategory.Technology);

        // Assert
        confidence.Should().Be(0f);
    }
}