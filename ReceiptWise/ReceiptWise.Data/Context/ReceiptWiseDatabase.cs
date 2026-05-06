namespace ReceiptWise.Data.Context;

using SQLite;
using ReceiptWise.Data.Entities;
using Microsoft.Extensions.Logging;

/// <summary>
/// SQLite database connection and initialization
/// Enhanced with logging and better error handling
/// </summary>
public class ReceiptWiseDatabase
{
    private readonly SQLiteAsyncConnection _database;
    private readonly ILogger<ReceiptWiseDatabase>? _logger;
    private bool _isInitialized;

    public ReceiptWiseDatabase(string dbPath, ILogger<ReceiptWiseDatabase>? logger = null)
    {
        _logger = logger;
        _database = new SQLiteAsyncConnection(dbPath);
        _logger?.LogInformation("Database initialized at: {DbPath}", dbPath);
    }

    public async Task InitializeAsync()
    {
        if (_isInitialized)
            return;

        try
        {
            _logger?.LogInformation("Creating database tables...");

            await _database.CreateTableAsync<ReceiptEntity>();
            await _database.CreateTableAsync<ReceiptItemEntity>();
            await _database.CreateTableAsync<AttachmentEntity>();
            await _database.CreateTableAsync<CategoryEntity>();
            await _database.CreateTableAsync<WarrantyInfoEntity>();

            // Create indexes for better query performance
            await _database.ExecuteAsync(
                "CREATE INDEX IF NOT EXISTS idx_receipts_date ON Receipts(TransactionDate DESC)");
            await _database.ExecuteAsync(
                "CREATE INDEX IF NOT EXISTS idx_receipts_merchant ON Receipts(MerchantName)");
            await _database.ExecuteAsync(
                "CREATE INDEX IF NOT EXISTS idx_receipts_category ON Receipts(Category)");
            await _database.ExecuteAsync(
                "CREATE INDEX IF NOT EXISTS idx_items_receipt ON ReceiptItems(ReceiptId)");
            await _database.ExecuteAsync(
                "CREATE INDEX IF NOT EXISTS idx_attachments_receipt ON Attachments(ReceiptId)");

            _isInitialized = true;
            _logger?.LogInformation("Database tables created successfully");
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to initialize database");
            throw;
        }
    }

    public SQLiteAsyncConnection GetConnection() => _database;

    public async Task<int> GetDatabaseVersionAsync()
    {
        return await _database.ExecuteScalarAsync<int>("PRAGMA user_version");
    }

    public async Task SetDatabaseVersionAsync(int version)
    {
        await _database.ExecuteAsync($"PRAGMA user_version = {version}");
    }

    public async Task<bool> TableExistsAsync(string tableName)
    {
        var result = await _database.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name=?", tableName);
        return result > 0;
    }

    public async Task ClearAllDataAsync()
    {
        await _database.DeleteAllAsync<ReceiptItemEntity>();
        await _database.DeleteAllAsync<AttachmentEntity>();
        await _database.DeleteAllAsync<WarrantyInfoEntity>();
        await _database.DeleteAllAsync<ReceiptEntity>();
        await _database.DeleteAllAsync<CategoryEntity>();

        _logger?.LogWarning("All data cleared from database");
    }
}