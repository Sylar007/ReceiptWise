namespace ReceiptWise.Tests.Unit;

using ReceiptWise.Data.Context;

/// <summary>
/// Base class for unit tests with in-memory database
/// </summary>
public abstract class TestBase : IDisposable
{
    protected ReceiptWiseDatabase Database { get; }
    protected string DbPath { get; }

    protected TestBase()
    {
        // Create unique in-memory database for each test
        DbPath = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid()}.db3");
        Database = new ReceiptWiseDatabase(DbPath);
        Database.InitializeAsync().Wait();
    }

    public void Dispose()
    {
        // Clean up test database
        if (File.Exists(DbPath))
        {
            File.Delete(DbPath);
        }
        GC.SuppressFinalize(this);
    }
}