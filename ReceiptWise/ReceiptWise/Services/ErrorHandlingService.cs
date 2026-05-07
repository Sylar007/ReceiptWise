namespace ReceiptWise.App.Services;

using Microsoft.Extensions.Logging;
using System.Diagnostics;

/// <summary>
/// Global error handling and user-friendly error messages
/// </summary>
public class ErrorHandlingService
{
    private readonly ILogger<ErrorHandlingService>? _logger;

    public ErrorHandlingService(ILogger<ErrorHandlingService>? logger = null)
    {
        _logger = logger;
    }

    /// <summary>
    /// Handle exception and return user-friendly message
    /// </summary>
    public string GetUserFriendlyMessage(Exception ex)
    {
        _logger?.LogError(ex, "Error occurred: {Message}", ex.Message);

        return ex switch
        {
            UnauthorizedAccessException => "Access denied. Please check your permissions.",
            HttpRequestException => "Network error. Please check your internet connection and try again.",
            TaskCanceledException => "The operation timed out. Please try again.",
            FileNotFoundException => "File not found. It may have been moved or deleted.",
            InvalidOperationException => "Invalid operation. Please try again or restart the app.",
            ArgumentException => "Invalid input. Please check your data and try again.",
            _ => "An unexpected error occurred. Please try again."
        };
    }

    /// <summary>
    /// Log error with context
    /// </summary>
    public void LogError(Exception ex, string context, Dictionary<string, object>? additionalData = null)
    {
        var errorId = Guid.NewGuid().ToString("N")[..8];

        _logger?.LogError(
            ex,
            "Error {ErrorId} in {Context}: {Message}",
            errorId,
            context,
            ex.Message);

        if (additionalData != null)
        {
            foreach (var kvp in additionalData)
            {
                _logger?.LogDebug("  {Key}: {Value}", kvp.Key, kvp.Value);
            }
        }

#if DEBUG
        Debug.WriteLine($"ERROR [{errorId}] {context}: {ex}");
#endif
    }

    /// <summary>
    /// Show error dialog to user
    /// </summary>
    public async Task ShowErrorDialogAsync(Exception ex, string title = "Error")
    {
        var message = GetUserFriendlyMessage(ex);
        await Shell.Current.DisplayAlert(title, message, "OK");
    }

    /// <summary>
    /// Show error dialog with retry option
    /// </summary>
    public async Task<bool> ShowErrorWithRetryAsync(Exception ex, string context)
    {
        var message = GetUserFriendlyMessage(ex);

        return await Shell.Current.DisplayAlert(
            "Error",
            $"{message}\n\nWould you like to try again?",
            "Retry",
            "Cancel");
    }
}