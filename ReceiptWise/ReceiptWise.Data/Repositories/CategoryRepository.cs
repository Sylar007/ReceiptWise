namespace ReceiptWise.Data.Repositories;

using ReceiptWise.Core.Interfaces.Repositories;
using ReceiptWise.Core.Models.Domain;
using ReceiptWise.Core.Enums;
using ReceiptWise.Data.Context;
using ReceiptWise.Data.Entities;

/// <summary>
/// SQLite implementation of ICategoryRepository
/// </summary>
public class CategoryRepository : ICategoryRepository
{
    private readonly ReceiptWiseDatabase _database;

    public CategoryRepository(ReceiptWiseDatabase database)
    {
        _database = database;
    }

    public async Task<IEnumerable<Category>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var conn = _database.GetConnection();
        var entities = await conn.Table<CategoryEntity>().ToListAsync();

        return entities.Select(e => new Category
        {
            Id = e.Id,
            CategoryType = (ReceiptCategory)e.CategoryType,
            DisplayName = e.DisplayName,
            Icon = e.Icon,
            Color = e.Color
        });
    }

    public async Task InitializeDefaultCategoriesAsync(CancellationToken cancellationToken = default)
    {
        var conn = _database.GetConnection();
        var count = await conn.Table<CategoryEntity>().CountAsync();

        if (count > 0)
            return; // Already initialized

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
}