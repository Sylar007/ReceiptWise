namespace ReceiptWise.App.Services;

using System.Diagnostics;
using Microsoft.Extensions.Logging;

/// <summary>
/// Monitor app performance and log metrics
/// </summary>
public class PerformanceMonitoringService
{
    private readonly ILogger<PerformanceMonitoringService>? _logger;
    private readonly Dictionary<string, Stopwatch> _activeOperations = new();

    public PerformanceMonitoringService(ILogger<PerformanceMonitoringService>? logger = null)
    {
        _logger = logger;
    }

    /// <summary>
    /// Start timing an operation
    /// </summary>
    public void StartOperation(string operationName)
    {
        var stopwatch = Stopwatch.StartNew();
        _activeOperations[operationName] = stopwatch;

        _logger?.LogDebug("Started operation: {Operation}", operationName);
    }

    /// <summary>
    /// Stop timing and log duration
    /// </summary>
    public void StopOperation(string operationName)
    {
        if (_activeOperations.TryGetValue(operationName, out var stopwatch))
        {
            stopwatch.Stop();
            _activeOperations.Remove(operationName);

            var duration = stopwatch.ElapsedMilliseconds;

            if (duration > 1000) // Log warning if > 1 second
            {
                _logger?.LogWarning(
                    "Operation {Operation} took {Duration}ms (slow)",
                    operationName,
                    duration);
            }
            else
            {
                _logger?.LogInformation(
                    "Operation {Operation} completed in {Duration}ms",
                    operationName,
                    duration);
            }
        }
    }

    /// <summary>
    /// Execute operation with automatic timing
    /// </summary>
    public async Task<T> MonitorAsync<T>(string operationName, Func<Task<T>> operation)
    {
        StartOperation(operationName);
        try
        {
            return await operation();
        }
        finally
        {
            StopOperation(operationName);
        }
    }

    /// <summary>
    /// Get memory usage statistics
    /// </summary>
    public MemoryStats GetMemoryStats()
    {
        var gcMemory = GC.GetTotalMemory(false);
        var gen0 = GC.CollectionCount(0);
        var gen1 = GC.CollectionCount(1);
        var gen2 = GC.CollectionCount(2);

        return new MemoryStats
        {
            ManagedMemoryMB = gcMemory / 1024.0 / 1024.0,
            Gen0Collections = gen0,
            Gen1Collections = gen1,
            Gen2Collections = gen2
        };
    }

    /// <summary>
    /// Log memory statistics
    /// </summary>
    public void LogMemoryStats()
    {
        var stats = GetMemoryStats();

        _logger?.LogInformation(
            "Memory: {Memory:F2} MB | GC: Gen0={Gen0}, Gen1={Gen1}, Gen2={Gen2}",
            stats.ManagedMemoryMB,
            stats.Gen0Collections,
            stats.Gen1Collections,
            stats.Gen2Collections);
    }
}

public class MemoryStats
{
    public double ManagedMemoryMB { get; set; }
    public int Gen0Collections { get; set; }
    public int Gen1Collections { get; set; }
    public int Gen2Collections { get; set; }
}