namespace ReceiptWise.Data.Context;

using SQLite;
using ReceiptWise.Data.Entities;

/// <summary>
/// SQLite database connection and initialization
/// </summary>
public class ReceiptWiseDatabase
{
    private readonly SQLiteAsyncConnection _database;

    public ReceiptWiseDatabase(string dbPath)
    {
        _database = new SQLiteAsyncConnection(dbPath);
        InitializeDatabaseAsync().Wait();
    }

    private async Task InitializeDatabaseAsync()
    {
        await _database.CreateTableAsync<ReceiptEntity>();
        await _database.CreateTableAsync<ReceiptItemEntity>();
        await _database.CreateTableAsync<AttachmentEntity>();
        await _database.CreateTableAsync<CategoryEntity>();
        await _database.CreateTableAsync<WarrantyInfoEntity>();
    }

    public SQLiteAsyncConnection GetConnection() => _database;
}