namespace ReceiptWise.App.ViewModels;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ReceiptWise.Core.Interfaces.Repositories;
using ReceiptWise.Core.Models.Domain;

/// <summary>
/// ViewModel for Receipt Detail page
/// </summary>
[QueryProperty(nameof(ReceiptId), "ReceiptId")]
public partial class ReceiptDetailViewModel : BaseViewModel
{
    private readonly IReceiptRepository _receiptRepository;

    [ObservableProperty]
    private int _receiptId;

    [ObservableProperty]
    private Receipt? _receipt;

    public ReceiptDetailViewModel(IReceiptRepository receiptRepository)
    {
        _receiptRepository = receiptRepository;
        Title = "Receipt Details";
    }

    partial void OnReceiptIdChanged(int value)
    {
        _ = LoadReceiptAsync();
    }

    [RelayCommand]
    private async Task LoadReceiptAsync()
    {
        if (IsBusy || ReceiptId == 0)
            return;

        try
        {
            IsBusy = true;
            ClearError();

            Receipt = await _receiptRepository.GetByIdAsync(ReceiptId);

            if (Receipt == null)
            {
                SetError("Receipt not found");
            }
        }
        catch (Exception ex)
        {
            SetError($"Failed to load receipt: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task DeleteReceiptAsync()
    {
        if (Receipt == null)
            return;

        bool confirm = await Shell.Current.DisplayAlert(
            "Delete Receipt",
            "Are you sure you want to delete this receipt?",
            "Yes",
            "No");

        if (!confirm)
            return;

        try
        {
            IsBusy = true;
            await _receiptRepository.DeleteAsync(Receipt.Id);
            await Shell.Current.GoToAsync("..");
        }
        catch (Exception ex)
        {
            SetError($"Failed to delete receipt: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }
}