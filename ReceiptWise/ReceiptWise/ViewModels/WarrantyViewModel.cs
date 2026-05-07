namespace ReceiptWise.App.ViewModels;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ReceiptWise.Core.Interfaces.Repositories;
using ReceiptWise.Core.Interfaces.Services;
using ReceiptWise.Core.Models.Domain;
using ReceiptWise.Data.Context;
using ReceiptWise.Data.Entities;

/// <summary>
/// ViewModel for managing warranty on receipt detail page
/// </summary>
[QueryProperty(nameof(ReceiptId), "ReceiptId")]
public partial class WarrantyViewModel : BaseViewModel
{
    private readonly ReceiptWiseDatabase _database;
    private readonly INotificationService _notificationService;

    [ObservableProperty]
    private int _receiptId;

    [ObservableProperty]
    private WarrantyInfo? _warranty;

    [ObservableProperty]
    private bool _hasWarranty;

    [ObservableProperty]
    private DateTime _purchaseDate = DateTime.Now;

    [ObservableProperty]
    private int _warrantyMonths = 12;

    [ObservableProperty]
    private DateTime _warrantyEndDate;

    [ObservableProperty]
    private string _productName = string.Empty;

    [ObservableProperty]
    private string _warrantyTerms = string.Empty;

    [ObservableProperty]
    private bool _notificationEnabled = true;

    [ObservableProperty]
    private int _daysRemaining;

    [ObservableProperty]
    private bool _isExpired;

    [ObservableProperty]
    private bool _isExpiringSoon;

    public WarrantyViewModel(
        ReceiptWiseDatabase database,
        INotificationService notificationService)
    {
        _database = database;
        _notificationService = notificationService;
        Title = "Warranty Information";

        // Initialize warranty end date
        CalculateWarrantyEndDate();
    }

    partial void OnReceiptIdChanged(int value)
    {
        _ = LoadWarrantyAsync();
    }

    partial void OnWarrantyMonthsChanged(int value)
    {
        CalculateWarrantyEndDate();
    }

    partial void OnPurchaseDateChanged(DateTime value)
    {
        CalculateWarrantyEndDate();
    }

    [RelayCommand]
    private async Task LoadWarrantyAsync()
    {
        if (ReceiptId == 0)
            return;

        try
        {
            var conn = _database.GetConnection();
            var warrantyEntity = await conn.Table<WarrantyInfoEntity>()
                .Where(w => w.ReceiptId == ReceiptId)
                .FirstOrDefaultAsync();

            if (warrantyEntity != null)
            {
                HasWarranty = true;
                Warranty = new WarrantyInfo
                {
                    Id = warrantyEntity.Id,
                    ReceiptId = warrantyEntity.ReceiptId,
                    PurchaseDate = warrantyEntity.PurchaseDate,
                    WarrantyEndDate = warrantyEntity.WarrantyEndDate,
                    WarrantyMonths = warrantyEntity.WarrantyMonths,
                    ProductName = warrantyEntity.ProductName,
                    WarrantyTerms = warrantyEntity.WarrantyTerms,
                    NotificationEnabled = warrantyEntity.NotificationEnabled
                };

                PurchaseDate = warrantyEntity.PurchaseDate;
                WarrantyMonths = warrantyEntity.WarrantyMonths;
                WarrantyEndDate = warrantyEntity.WarrantyEndDate;
                ProductName = warrantyEntity.ProductName ?? string.Empty;
                WarrantyTerms = warrantyEntity.WarrantyTerms ?? string.Empty;
                NotificationEnabled = warrantyEntity.NotificationEnabled;

                UpdateWarrantyStatus();
            }
            else
            {
                HasWarranty = false;
            }
        }
        catch (Exception ex)
        {
            SetError($"Failed to load warranty: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task SaveWarrantyAsync()
    {
        try
        {
            if (string.IsNullOrWhiteSpace(ProductName))
            {
                SetError("Product name is required");
                return;
            }

            if (WarrantyMonths <= 0)
            {
                SetError("Warranty period must be greater than 0");
                return;
            }

            IsBusy = true;
            ClearError();

            var conn = _database.GetConnection();
            var warrantyEntity = new WarrantyInfoEntity
            {
                ReceiptId = ReceiptId,
                PurchaseDate = PurchaseDate,
                WarrantyEndDate = WarrantyEndDate,
                WarrantyMonths = WarrantyMonths,
                ProductName = ProductName.Trim(),
                WarrantyTerms = string.IsNullOrWhiteSpace(WarrantyTerms) ? null : WarrantyTerms.Trim(),
                NotificationEnabled = NotificationEnabled
            };

            if (HasWarranty && Warranty != null)
            {
                // Update existing
                warrantyEntity.Id = Warranty.Id;
                await conn.UpdateAsync(warrantyEntity);
            }
            else
            {
                // Insert new
                await conn.InsertAsync(warrantyEntity);
                HasWarranty = true;
            }

            // Schedule notification if enabled
            if (NotificationEnabled)
            {
                await _notificationService.ScheduleWarrantyReminderAsync(
                    ReceiptId,
                    ProductName,
                    WarrantyEndDate);
            }
            else
            {
                await _notificationService.CancelNotificationAsync(ReceiptId);
            }

            await Shell.Current.DisplayAlert("Success", "Warranty information saved", "OK");
            await LoadWarrantyAsync();
        }
        catch (Exception ex)
        {
            SetError($"Failed to save warranty: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task DeleteWarrantyAsync()
    {
        if (!HasWarranty || Warranty == null)
            return;

        bool confirm = await Shell.Current.DisplayAlert(
            "Delete Warranty",
            "Are you sure you want to remove warranty information?",
            "Yes",
            "No");

        if (!confirm)
            return;

        try
        {
            IsBusy = true;

            var conn = _database.GetConnection();
            await conn.DeleteAsync<WarrantyInfoEntity>(Warranty.Id);

            // Cancel notification
            await _notificationService.CancelNotificationAsync(ReceiptId);

            HasWarranty = false;
            Warranty = null;
            ProductName = string.Empty;
            WarrantyTerms = string.Empty;

            await Shell.Current.DisplayAlert("Success", "Warranty information removed", "OK");
        }
        catch (Exception ex)
        {
            SetError($"Failed to delete warranty: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task TestNotificationAsync()
    {
        try
        {
            if (string.IsNullOrWhiteSpace(ProductName))
            {
                await Shell.Current.DisplayAlert("Error", "Please enter a product name first", "OK");
                return;
            }

            // Schedule a test notification for 5 seconds from now
            var testDate = DateTime.Now.AddSeconds(5);
            await _notificationService.ScheduleWarrantyReminderAsync(
                ReceiptId,
                ProductName,
                testDate);

            await Shell.Current.DisplayAlert(
                "Test Scheduled",
                "Test notification will appear in 5 seconds",
                "OK");
        }
        catch (Exception ex)
        {
            SetError($"Failed to schedule test: {ex.Message}");
        }
    }

    private void CalculateWarrantyEndDate()
    {
        WarrantyEndDate = PurchaseDate.AddMonths(WarrantyMonths);
        UpdateWarrantyStatus();
    }

    private void UpdateWarrantyStatus()
    {
        var today = DateTime.Now.Date;
        var endDate = WarrantyEndDate.Date;

        DaysRemaining = (endDate - today).Days;
        IsExpired = DaysRemaining < 0;
        IsExpiringSoon = DaysRemaining >= 0 && DaysRemaining <= 30;
    }
}