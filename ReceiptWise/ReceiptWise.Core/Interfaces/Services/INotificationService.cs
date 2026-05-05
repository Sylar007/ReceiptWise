namespace ReceiptWise.Core.Interfaces.Services;

/// <summary>
/// Service for local notifications
/// </summary>
public interface INotificationService
{
    Task ScheduleWarrantyReminderAsync(
        int receiptId,
        string productName,
        DateTime warrantyEndDate,
        CancellationToken cancellationToken = default);

    Task CancelNotificationAsync(int receiptId, CancellationToken cancellationToken = default);
}