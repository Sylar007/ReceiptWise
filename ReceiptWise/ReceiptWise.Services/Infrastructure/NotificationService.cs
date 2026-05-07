namespace ReceiptWise.Services.Infrastructure;

using Microsoft.Extensions.Logging;
using ReceiptWise.Core.Interfaces.Services;

/// <summary>
/// Cross-platform notification service for local notifications
/// Handles warranty expiry reminders
/// </summary>
public class NotificationService : INotificationService
{
    private readonly ILogger<NotificationService>? _logger;

    public NotificationService(ILogger<NotificationService>? logger = null)
    {
        _logger = logger;
    }

    /// <summary>
    /// Schedule a warranty reminder notification
    /// Sends notification 7 days before warranty expires
    /// </summary>
    public async Task ScheduleWarrantyReminderAsync(
        int receiptId,
        string productName,
        DateTime warrantyEndDate,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Calculate reminder date (7 days before expiry)
            var reminderDate = warrantyEndDate.AddDays(-7);

            // Don't schedule if already expired or reminder date is in the past
            if (reminderDate <= DateTime.Now)
            {
                _logger?.LogInformation(
                    "Skipping reminder for receipt {ReceiptId} - reminder date in the past",
                    receiptId);
                return;
            }

            _logger?.LogInformation(
                "Scheduling warranty reminder for receipt {ReceiptId}, product: {Product}, date: {Date}",
                receiptId,
                productName,
                reminderDate);

#if ANDROID
            await ScheduleAndroidNotificationAsync(receiptId, productName, warrantyEndDate, reminderDate);
#elif IOS || MACCATALYST
            await ScheduleIosNotificationAsync(receiptId, productName, warrantyEndDate, reminderDate);
#else
            _logger?.LogWarning("Notifications not supported on this platform");
            await Task.CompletedTask;
#endif
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to schedule warranty reminder for receipt {ReceiptId}", receiptId);
            throw;
        }
    }

    /// <summary>
    /// Cancel a scheduled notification
    /// </summary>
    public async Task CancelNotificationAsync(int receiptId, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger?.LogInformation("Cancelling notification for receipt {ReceiptId}", receiptId);

#if ANDROID
            await CancelAndroidNotificationAsync(receiptId);
#elif IOS || MACCATALYST
            await CancelIosNotificationAsync(receiptId);
#else
            await Task.CompletedTask;
#endif
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to cancel notification for receipt {ReceiptId}", receiptId);
        }
    }

#if ANDROID
    private async Task ScheduleAndroidNotificationAsync(
        int receiptId,
        string productName,
        DateTime warrantyEndDate,
        DateTime reminderDate)
    {
        await Task.Run(() =>
        {
            var notificationId = receiptId;
            
            var title = "⚠️ Warranty Expiring Soon";
            var message = $"{productName} warranty expires on {warrantyEndDate:MMM dd, yyyy}";

            // Use Android's AlarmManager for scheduled notifications
            var intent = new Android.Content.Intent(Android.App.Application.Context, typeof(WarrantyNotificationReceiver));
            intent.PutExtra("notificationId", notificationId);
            intent.PutExtra("title", title);
            intent.PutExtra("message", message);
            intent.PutExtra("receiptId", receiptId);

            var pendingIntent = Android.App.PendingIntent.GetBroadcast(
                Android.App.Application.Context,
                notificationId,
                intent,
                Android.App.PendingIntentFlags.UpdateCurrent | Android.App.PendingIntentFlags.Immutable);

            var alarmManager = (Android.App.AlarmManager?)Android.App.Application.Context
                .GetSystemService(Android.Content.Context.AlarmService);

            if (alarmManager != null)
            {
                var triggerTime = new DateTimeOffset(reminderDate).ToUnixTimeMilliseconds();
                alarmManager.SetExact(
                    Android.App.AlarmType.RtcWakeup,
                    triggerTime,
                    pendingIntent);

                _logger?.LogInformation("Android notification scheduled for {Date}", reminderDate);
            }
        });
    }

    private async Task CancelAndroidNotificationAsync(int receiptId)
    {
        await Task.Run(() =>
        {
            var intent = new Android.Content.Intent(Android.App.Application.Context, typeof(WarrantyNotificationReceiver));
            var pendingIntent = Android.App.PendingIntent.GetBroadcast(
                Android.App.Application.Context,
                receiptId,
                intent,
                Android.App.PendingIntentFlags.UpdateCurrent | Android.App.PendingIntentFlags.Immutable);

            var alarmManager = (Android.App.AlarmManager?)Android.App.Application.Context
                .GetSystemService(Android.Content.Context.AlarmService);

            alarmManager?.Cancel(pendingIntent);
            pendingIntent?.Cancel();
        });
    }
#endif

#if IOS || MACCATALYST
    private async Task ScheduleIosNotificationAsync(
        int receiptId,
        string productName,
        DateTime warrantyEndDate,
        DateTime reminderDate)
    {
        var center = UserNotifications.UNUserNotificationCenter.Current;

        // Request permission
        var (granted, error) = await center.RequestAuthorizationAsync(
            UserNotifications.UNAuthorizationOptions.Alert | 
            UserNotifications.UNAuthorizationOptions.Badge | 
            UserNotifications.UNAuthorizationOptions.Sound);

        if (!granted)
        {
            _logger?.LogWarning("Notification permission not granted");
            return;
        }

        // Create notification content
        var content = new UserNotifications.UNMutableNotificationContent
        {
            Title = "⚠️ Warranty Expiring Soon",
            Body = $"{productName} warranty expires on {warrantyEndDate:MMM dd, yyyy}",
            Sound = UserNotifications.UNNotificationSound.Default,
            Badge = 1
        };

        // Create date trigger
        var dateComponents = new Foundation.NSDateComponents
        {
            Year = reminderDate.Year,
            Month = reminderDate.Month,
            Day = reminderDate.Day,
            Hour = 9, // 9 AM
            Minute = 0
        };

        var trigger = UserNotifications.UNCalendarNotificationTrigger.CreateTrigger(dateComponents, false);

        // Create request
        var request = UserNotifications.UNNotificationRequest.FromIdentifier(
            $"warranty_{receiptId}",
            content,
            trigger);

        // Schedule notification
        await center.AddNotificationRequestAsync(request);

        _logger?.LogInformation("iOS notification scheduled for {Date}", reminderDate);
    }

    private async Task CancelIosNotificationAsync(int receiptId)
    {
        var center = UserNotifications.UNUserNotificationCenter.Current;
        var identifiers = new[] { $"warranty_{receiptId}" };
        center.RemovePendingNotificationRequests(identifiers);
        await Task.CompletedTask;
    }
#endif

    /// <summary>
    /// Get all scheduled notification identifiers
    /// </summary>
    public async Task<IEnumerable<string>> GetScheduledNotificationsAsync()
    {
#if IOS || MACCATALYST
        var center = UserNotifications.UNUserNotificationCenter.Current;
        var requests = await center.GetPendingNotificationRequestsAsync();
        return requests.Select(r => r.Identifier);
#else
        return await Task.FromResult(Enumerable.Empty<string>());
#endif
    }
}