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

            // Create tables one by one with individual error handling
            try
            {
                System.Diagnostics.Debug.WriteLine("→ Creating ReceiptEntity table...");
                await _database.CreateTableAsync<ReceiptEntity>();
                System.Diagnostics.Debug.WriteLine("✓ ReceiptEntity table created");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"✗ FAILED: ReceiptEntity - {ex.GetType().Name}: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack: {ex.StackTrace}");
                throw new Exception($"Failed to create ReceiptEntity table: {ex.Message}", ex);
            }

            try
            {
                System.Diagnostics.Debug.WriteLine("→ Creating ReceiptItemEntity table...");
                await _database.CreateTableAsync<ReceiptItemEntity>();
                System.Diagnostics.Debug.WriteLine("✓ ReceiptItemEntity table created");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"✗ FAILED: ReceiptItemEntity - {ex.GetType().Name}: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack: {ex.StackTrace}");
                throw new Exception($"Failed to create ReceiptItemEntity table: {ex.Message}", ex);
            }

            try
            {
                System.Diagnostics.Debug.WriteLine("→ Creating AttachmentEntity table...");
                await _database.CreateTableAsync<AttachmentEntity>();
                System.Diagnostics.Debug.WriteLine("✓ AttachmentEntity table created");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"✗ FAILED: AttachmentEntity - {ex.GetType().Name}: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack: {ex.StackTrace}");
                throw new Exception($"Failed to create AttachmentEntity table: {ex.Message}", ex);
            }

            try
            {
                System.Diagnostics.Debug.WriteLine("→ Creating CategoryEntity table...");
                await _database.CreateTableAsync<CategoryEntity>();
                System.Diagnostics.Debug.WriteLine("✓ CategoryEntity table created");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"✗ FAILED: CategoryEntity - {ex.GetType().Name}: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack: {ex.StackTrace}");
                throw new Exception($"Failed to create CategoryEntity table: {ex.Message}", ex);
            }

            try
            {
                System.Diagnostics.Debug.WriteLine("→ Creating WarrantyInfoEntity table...");
                await _database.CreateTableAsync<WarrantyInfoEntity>();
                System.Diagnostics.Debug.WriteLine("✓ WarrantyInfoEntity table created");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"✗ FAILED: WarrantyInfoEntity - {ex.GetType().Name}: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack: {ex.StackTrace}");
                throw new Exception($"Failed to create WarrantyInfoEntity table: {ex.Message}", ex);
            }

            // Create indexes for better query performance
            System.Diagnostics.Debug.WriteLine("→ Creating indexes...");
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
            System.Diagnostics.Debug.WriteLine("✓ Indexes created");

            _isInitialized = true;
            _logger?.LogInformation("Database tables created successfully");
            System.Diagnostics.Debug.WriteLine("✓✓✓ ALL TABLES CREATED SUCCESSFULLY ✓✓✓");
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to initialize database");
            System.Diagnostics.Debug.WriteLine($"════════ DATABASE INITIALIZATION FAILED ════════");
            System.Diagnostics.Debug.WriteLine($"Exception Type: {ex.GetType().FullName}");
            System.Diagnostics.Debug.WriteLine($"Message: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"Stack Trace:\n{ex.StackTrace}");
            if (ex.InnerException != null)
            {
                System.Diagnostics.Debug.WriteLine($"Inner Exception: {ex.InnerException.GetType().FullName}");
                System.Diagnostics.Debug.WriteLine($"Inner Message: {ex.InnerException.Message}");
                System.Diagnostics.Debug.WriteLine($"Inner Stack:\n{ex.InnerException.StackTrace}");
            }
            System.Diagnostics.Debug.WriteLine($"═══════════════════════════════════════════════");
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