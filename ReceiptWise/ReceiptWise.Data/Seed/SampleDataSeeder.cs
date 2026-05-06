namespace ReceiptWise.Data.Seed;

using ReceiptWise.Core.Enums;
using ReceiptWise.Data.Context;
using ReceiptWise.Data.Entities;

/// <summary>
/// Seeds sample receipt data for testing and demonstration
/// </summary>
public class SampleDataSeeder
{
    private readonly ReceiptWiseDatabase _database;

    public SampleDataSeeder(ReceiptWiseDatabase database)
    {
        _database = database;
    }

    public async Task SeedSampleReceiptsAsync(int count = 10)
    {
        var conn = _database.GetConnection();

        // Check if data already exists
        var existingCount = await conn.Table<ReceiptEntity>().CountAsync();
        if (existingCount > 0)
            return; // Already seeded

        var random = new Random(42); // Fixed seed for consistency
        var merchants = new[]
        {
            ("Whole Foods Market", ReceiptCategory.Groceries),
            ("Walmart Supercenter", ReceiptCategory.Groceries),
            ("Starbucks Coffee", ReceiptCategory.Dining),
            ("McDonald's", ReceiptCategory.Dining),
            ("Shell Gas Station", ReceiptCategory.Transportation),
            ("Amazon.com", ReceiptCategory.Shopping),
            ("Best Buy", ReceiptCategory.Technology),
            ("CVS Pharmacy", ReceiptCategory.Healthcare),
            ("Home Depot", ReceiptCategory.HomeAndGarden),
            ("AMC Theatres", ReceiptCategory.Entertainment),
            ("Uber", ReceiptCategory.Transportation),
            ("Target", ReceiptCategory.Shopping)
        };

        var items = new[]
        {
            "Organic Bananas", "Milk 2%", "Bread Whole Wheat", "Coffee Beans",
            "Chicken Breast", "Pasta", "Tomato Sauce", "Cheese", "Eggs",
            "Big Mac Meal", "Latte Grande", "Gas Regular", "USB Cable",
            "Headphones", "Vitamins", "Paint Brush", "Movie Ticket"
        };

        for (int i = 0; i < count; i++)
        {
            var (merchantName, category) = merchants[random.Next(merchants.Length)];
            var transactionDate = DateTime.Now.AddDays(-random.Next(90));
            var itemCount = random.Next(1, 6);
            var subtotal = 0m;

            // Create receipt
            var receipt = new ReceiptEntity
            {
                MerchantName = merchantName,
                TransactionDate = transactionDate,
                Category = (int)category,
                Currency = (int)CurrencyCode.USD,
                ExtractionStatus = (int)ExtractionStatus.ManualEntry,
                Notes = random.Next(10) > 7 ? "Sample receipt for testing" : null,
                CreatedAt = transactionDate,
                ModifiedAt = null
            };

            // Calculate items
            var receiptItems = new List<ReceiptItemEntity>();
            for (int j = 0; j < itemCount; j++)
            {
                var quantity = random.Next(1, 4);
                var unitPrice = (decimal)(random.NextDouble() * 50 + 1);
                var totalPrice = quantity * unitPrice;
                subtotal += totalPrice;

                receiptItems.Add(new ReceiptItemEntity
                {
                    Description = items[random.Next(items.Length)],
                    Quantity = quantity,
                    UnitPrice = Math.Round(unitPrice, 2),
                    TotalPrice = Math.Round(totalPrice, 2)
                });
            }

            receipt.Subtotal = Math.Round(subtotal, 2);
            receipt.Tax = Math.Round(subtotal * 0.0875m, 2); // 8.75% tax
            receipt.Total = Math.Round(receipt.Subtotal + receipt.Tax, 2);

            // Insert receipt
            await conn.InsertAsync(receipt);

            // Insert items
            foreach (var item in receiptItems)
            {
                item.ReceiptId = receipt.Id;
                await conn.InsertAsync(item);
            }

            // 30% chance of having a warranty
            if (random.Next(10) < 3 && category == ReceiptCategory.Technology)
            {
                var warranty = new WarrantyInfoEntity
                {
                    ReceiptId = receipt.Id,
                    PurchaseDate = transactionDate,
                    WarrantyMonths = random.Next(1, 4) * 12, // 1-3 years
                    ProductName = "Electronic Device",
                    WarrantyTerms = "Manufacturer warranty covers defects",
                    NotificationEnabled = true
                };
                warranty.WarrantyEndDate = warranty.PurchaseDate.AddMonths(warranty.WarrantyMonths);
                await conn.InsertAsync(warranty);
            }
        }
    }

    public async Task SeedCategoriesAsync()
    {
        var conn = _database.GetConnection();
        var count = await conn.Table<CategoryEntity>().CountAsync();

        if (count > 0)
            return; // Already seeded

        var categories = new[]
        {
            new CategoryEntity { CategoryType = (int)ReceiptCategory.Groceries, DisplayName = "Groceries", Icon = "🛒", Color = "#4CAF50" },
            new CategoryEntity { CategoryType = (int)ReceiptCategory.Dining, DisplayName = "Dining", Icon = "🍽️", Color = "#FF9800" },
            new CategoryEntity { CategoryType = (int)ReceiptCategory.Transportation, DisplayName = "Transportation", Icon = "🚗", Color = "#2196F3" },
            new CategoryEntity { CategoryType = (int)ReceiptCategory.Shopping, DisplayName = "Shopping", Icon = "🛍️", Color = "#E91E63" },
            new CategoryEntity { CategoryType = (int)ReceiptCategory.Healthcare, DisplayName = "Healthcare", Icon = "🏥", Color = "#F44336" },
            new CategoryEntity { CategoryType = (int)ReceiptCategory.Utilities, DisplayName = "Utilities", Icon = "💡", Color = "#9C27B0" },
            new CategoryEntity { CategoryType = (int)ReceiptCategory.Entertainment, DisplayName = "Entertainment", Icon = "🎬", Color = "#FF5722" },
            new CategoryEntity { CategoryType = (int)ReceiptCategory.Travel, DisplayName = "Travel", Icon = "✈️", Color = "#00BCD4" },
            new CategoryEntity { CategoryType = (int)ReceiptCategory.HomeAndGarden, DisplayName = "Home & Garden", Icon = "🏡", Color = "#8BC34A" },
            new CategoryEntity { CategoryType = (int)ReceiptCategory.Technology, DisplayName = "Technology", Icon = "💻", Color = "#607D8B" },
            new CategoryEntity { CategoryType = (int)ReceiptCategory.Services, DisplayName = "Services", Icon = "🔧", Color = "#795548" },
            new CategoryEntity { CategoryType = (int)ReceiptCategory.Other, DisplayName = "Other", Icon = "📦", Color = "#9E9E9E" }
        };

        await conn.InsertAllAsync(categories);
    }

    public async Task<int> GetReceiptCountAsync()
    {
        var conn = _database.GetConnection();
        return await conn.Table<ReceiptEntity>().CountAsync();
    }

    public async Task<decimal> GetTotalSpentAsync()
    {
        var conn = _database.GetConnection();
        var receipts = await conn.Table<ReceiptEntity>().ToListAsync();
        return receipts.Sum(r => r.Total);
    }
}