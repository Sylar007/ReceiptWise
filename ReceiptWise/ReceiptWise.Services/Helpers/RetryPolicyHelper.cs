namespace ReceiptWise.Services.Helpers;

using Azure;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Retry;
using Polly.Timeout;

/// <summary>
/// Helper for creating resilient retry policies with Polly
/// Implements exponential backoff for transient failures
/// </summary>
public static class RetryPolicyHelper
{
    /// <summary>
    /// Create retry policy for Azure AI services
    /// Handles transient errors: 429 (rate limit), 503 (service unavailable), network errors
    /// </summary>
    public static AsyncRetryPolicy<T> CreateAzureRetryPolicy<T>(
        int maxRetries = 3,
        ILogger? logger = null)
    {
        return Policy<T>
            .Handle<RequestFailedException>(ex =>
                ex.Status == 429 || // Rate limit
                ex.Status == 503 || // Service unavailable
                ex.Status == 504)   // Gateway timeout
            .Or<HttpRequestException>()
            .Or<TaskCanceledException>()
            .WaitAndRetryAsync(
                maxRetries,
                retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)), // Exponential backoff: 2s, 4s, 8s
                onRetry: (outcome, timespan, retryCount, context) =>
                {
                    logger?.LogWarning(
                        "Retry {RetryCount}/{MaxRetries} after {Delay}s due to: {Error}",
                        retryCount,
                        maxRetries,
                        timespan.TotalSeconds,
                        outcome.Exception?.Message ?? "Unknown error");
                });
    }

    /// <summary>
    /// Create timeout policy for long-running operations
    /// </summary>
    public static AsyncTimeoutPolicy CreateTimeoutPolicy(
        int timeoutSeconds = 30,
        ILogger? logger = null)
    {
        return Policy.TimeoutAsync(
            TimeSpan.FromSeconds(timeoutSeconds),
            onTimeoutAsync: (context, timespan, task) =>
            {
                logger?.LogWarning("Operation timed out after {Timeout}s", timespan.TotalSeconds);
                return Task.CompletedTask;
            });
    }

    /// <summary>
    /// Create combined policy: retry + timeout
    /// </summary>
    public static IAsyncPolicy<T> CreateResilientPolicy<T>(
        int maxRetries = 3,
        int timeoutSeconds = 30,
        ILogger? logger = null)
    {
        var retryPolicy = CreateAzureRetryPolicy<T>(maxRetries, logger);
        var timeoutPolicy = Policy.TimeoutAsync<T>(
            TimeSpan.FromSeconds(timeoutSeconds),
            onTimeoutAsync: (context, timespan, task) =>
            {
                logger?.LogWarning("Operation timed out after {Timeout}s", timespan.TotalSeconds);
                return Task.CompletedTask;
            });

        // Wrap retry policy with timeout
        return Policy.WrapAsync<T>(retryPolicy, timeoutPolicy);
    }
}