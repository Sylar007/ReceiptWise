#if ANDROID
namespace ReceiptWise.App.Platforms.Android;

using AndroidX.Core.App;
using global::Android.App;
using global::Android.Content;

/// <summary>
/// BroadcastReceiver for handling scheduled warranty notifications on Android
/// </summary>
[BroadcastReceiver(Enabled = true, Exported = true)]
public class WarrantyNotificationReceiver : BroadcastReceiver
{
    public override void OnReceive(Context? context, Intent? intent)
    {
        if (context == null || intent == null)
            return;

        var notificationId = intent.GetIntExtra("notificationId", 0);
        var title = intent.GetStringExtra("title") ?? "Warranty Reminder";
        var message = intent.GetStringExtra("message") ?? "Check your warranty";
        var receiptId = intent.GetIntExtra("receiptId", 0);

        ShowNotification(context, notificationId, title, message, receiptId);
    }

    private void ShowNotification(Context context, int notificationId, string title, string message, int receiptId)
    {
        const string channelId = "warranty_reminders";
        const string channelName = "Warranty Reminders";

        // Create notification channel (required for Android 8.0+)
        if (OperatingSystem.IsAndroidVersionAtLeast(26))
        {
            var channel = new NotificationChannel(channelId, channelName, NotificationImportance.High)
            {
                Description = "Notifications for warranty expiry reminders"
            };

            var notificationManager = (NotificationManager?)context.GetSystemService(Context.NotificationService);
            notificationManager?.CreateNotificationChannel(channel);
        }

        // Create intent to open app when notification is tapped
        var notificationIntent = new Intent(context, typeof(MainActivity));
        notificationIntent.SetFlags(ActivityFlags.NewTask | ActivityFlags.ClearTask);
        notificationIntent.PutExtra("receiptId", receiptId);

        var pendingIntent = PendingIntent.GetActivity(
            context,
            notificationId,
            notificationIntent,
            PendingIntentFlags.UpdateCurrent | PendingIntentFlags.Immutable);

        // Build notification
        var notification = new NotificationCompat.Builder(context, channelId)
            .SetContentTitle(title)
            .SetContentText(message)
            .SetSmallIcon(Resource.Drawable.notification_icon_background)
            .SetAutoCancel(true)
            .SetPriority(NotificationCompat.PriorityHigh)
            .SetContentIntent(pendingIntent)
            .SetStyle(new NotificationCompat.BigTextStyle().BigText(message))
            .Build();

        var manager = NotificationManagerCompat.From(context);
        manager.Notify(notificationId, notification);
    }
}
#endif