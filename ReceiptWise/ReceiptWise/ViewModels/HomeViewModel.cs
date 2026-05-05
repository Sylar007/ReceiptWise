namespace ReceiptWise.App.ViewModels;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ReceiptWise.Core.Interfaces.Repositories;

/// <summary>
/// ViewModel for Home/Dashboard page
/// </summary>
public partial class HomeViewModel : BaseViewModel
{
    private readonly IReceiptRepository _receiptRepository;

    [ObservableProperty]
    private decimal _thisMonthTotal;

    [ObservableProperty]
    private int _receiptCount;

    public HomeViewModel(IReceiptRepository receiptRepository)
    {
        _receiptRepository = receiptRepository;
        Title = "Dashboard";
    }

    [RelayCommand]
    private async Task LoadDashboardAsync()
    {
        if (IsBusy)
            return;

        try
        {
            IsBusy = true;
            ClearError();

            var startOfMonth = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            var endOfMonth = startOfMonth.AddMonths(1).AddDays(-1);

            var receipts = await _receiptRepository.GetByDateRangeAsync(startOfMonth, endOfMonth);

            ThisMonthTotal = receipts.Sum(r => r.Total);
            ReceiptCount = receipts.Count();
        }
        catch (Exception ex)
        {
            SetError($"Failed to load dashboard: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }
}