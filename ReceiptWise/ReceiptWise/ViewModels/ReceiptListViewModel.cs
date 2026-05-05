namespace ReceiptWise.App.ViewModels;

using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ReceiptWise.Core.Interfaces.Repositories;
using ReceiptWise.Core.Models.Domain;

/// <summary>
/// ViewModel for Receipt List page
/// </summary>
public partial class ReceiptListViewModel : BaseViewModel
{
    private readonly IReceiptRepository _receiptRepository;

    [ObservableProperty]
    private ObservableCollection<Receipt> _receipts = new();

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private bool _isEmpty;

    public ReceiptListViewModel(IReceiptRepository receiptRepository)
    {
        _receiptRepository = receiptRepository;
        Title = "Receipts";
    }

    [RelayCommand]
    private async Task LoadReceiptsAsync()
    {
        if (IsBusy)
            return;

        try
        {
            IsBusy = true;
            ClearError();

            var receipts = await _receiptRepository.GetAllAsync();

            Receipts.Clear();
            foreach (var receipt in receipts)
            {
                Receipts.Add(receipt);
            }

            IsEmpty = !Receipts.Any();
        }
        catch (Exception ex)
        {
            SetError($"Failed to load receipts: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task SearchAsync()
    {
        if (IsBusy)
            return;

        try
        {
            IsBusy = true;
            ClearError();

            var receipts = await _receiptRepository.SearchAsync(searchTerm: SearchText);

            Receipts.Clear();
            foreach (var receipt in receipts)
            {
                Receipts.Add(receipt);
            }

            IsEmpty = !Receipts.Any();
        }
        catch (Exception ex)
        {
            SetError($"Search failed: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task ViewDetailAsync(Receipt receipt)
    {
        if (receipt == null)
            return;

        await Shell.Current.GoToAsync($"{nameof(ReceiptDetailPage)}?ReceiptId={receipt.Id}");
    }
}