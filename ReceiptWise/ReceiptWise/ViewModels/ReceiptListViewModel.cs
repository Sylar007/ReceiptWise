namespace ReceiptWise.App.ViewModels;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ReceiptWise.App.Views;
using ReceiptWise.Core.Enums;
using ReceiptWise.Core.Interfaces.Repositories;
using ReceiptWise.Core.Models.Domain;
using System.Collections.ObjectModel;
using System.Globalization;

/// <summary>
/// ViewModel for Receipt List page
/// Enhanced with advanced search, filtering, and sorting
/// </summary>
public partial class ReceiptListViewModel : BaseViewModel
{
    private readonly IReceiptRepository _receiptRepository;
    private readonly ICategoryRepository _categoryRepository;
    private List<Receipt> _allReceipts = new();

    [ObservableProperty]
    private ObservableCollection<Receipt> _receipts = new();

    [ObservableProperty]
    private ObservableCollection<Receipt> _selectedReceipts = new();

    [ObservableProperty]
    private ObservableCollection<Category> _availableCategories = new();

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private bool _isEmpty;

    [ObservableProperty]
    private bool _isSelectionMode;

    [ObservableProperty]
    private int _totalReceiptCount;

    [ObservableProperty]
    private decimal _totalAmount;

    [ObservableProperty]
    private string _filterStatus = "All Receipts";

    // Filter properties
    [ObservableProperty]
    private ReceiptCategory? _selectedCategory;

    [ObservableProperty]
    private DateTime? _startDate;

    [ObservableProperty]
    private DateTime? _endDate;

    [ObservableProperty]
    private decimal? _minAmount;

    [ObservableProperty]
    private decimal? _maxAmount;

    [ObservableProperty]
    private string _sortBy = "Date (Newest)";

    public ReceiptListViewModel(
        IReceiptRepository receiptRepository,
        ICategoryRepository categoryRepository)
    {
        _receiptRepository = receiptRepository;
        _categoryRepository = categoryRepository;
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

            // Load all receipts
            var receipts = await _receiptRepository.GetAllAsync();
            _allReceipts = receipts.ToList();

            // Load categories
            var categories = await _categoryRepository.GetAllAsync();
            AvailableCategories.Clear();
            foreach (var category in categories)
            {
                AvailableCategories.Add(category);
            }

            // Apply current filters
            await ApplyFiltersAsync();

            UpdateStatistics();
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
        await ApplyFiltersAsync();
    }

    [RelayCommand]
    private async Task ApplyFiltersAsync()
    {
        try
        {
            var filtered = _allReceipts.AsEnumerable();

            // Text search
            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                filtered = filtered.Where(r =>
                    r.MerchantName.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                    (r.Notes?.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ?? false));
            }

            // Category filter
            if (SelectedCategory.HasValue)
            {
                filtered = filtered.Where(r => r.Category == SelectedCategory.Value);
            }

            // Date range filter
            if (StartDate.HasValue)
            {
                filtered = filtered.Where(r => r.TransactionDate.Date >= StartDate.Value.Date);
            }

            if (EndDate.HasValue)
            {
                filtered = filtered.Where(r => r.TransactionDate.Date <= EndDate.Value.Date);
            }

            // Amount range filter
            if (MinAmount.HasValue)
            {
                filtered = filtered.Where(r => r.Total >= MinAmount.Value);
            }

            if (MaxAmount.HasValue)
            {
                filtered = filtered.Where(r => r.Total <= MaxAmount.Value);
            }

            // Apply sorting
            filtered = SortBy switch
            {
                "Date (Newest)" => filtered.OrderByDescending(r => r.TransactionDate),
                "Date (Oldest)" => filtered.OrderBy(r => r.TransactionDate),
                "Amount (Highest)" => filtered.OrderByDescending(r => r.Total),
                "Amount (Lowest)" => filtered.OrderBy(r => r.Total),
                "Merchant (A-Z)" => filtered.OrderBy(r => r.MerchantName),
                "Merchant (Z-A)" => filtered.OrderByDescending(r => r.MerchantName),
                _ => filtered.OrderByDescending(r => r.TransactionDate)
            };

            Receipts.Clear();
            foreach (var receipt in filtered)
            {
                Receipts.Add(receipt);
            }

            IsEmpty = !Receipts.Any();
            UpdateFilterStatus();
        }
        catch (Exception ex)
        {
            SetError($"Failed to apply filters: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task ClearFiltersAsync()
    {
        SearchText = string.Empty;
        SelectedCategory = null;
        StartDate = null;
        EndDate = null;
        MinAmount = null;
        MaxAmount = null;
        SortBy = "Date (Newest)";

        await ApplyFiltersAsync();
    }

    [RelayCommand]
    private async Task ViewDetailAsync(Receipt receipt)
    {
        if (receipt == null)
            return;

        if (IsSelectionMode)
        {
            // Toggle selection
            if (SelectedReceipts.Contains(receipt))
                SelectedReceipts.Remove(receipt);
            else
                SelectedReceipts.Add(receipt);
            return;
        }

        await Shell.Current.GoToAsync($"{nameof(ReceiptDetailPage)}?ReceiptId={receipt.Id}");
    }

    [RelayCommand]
    private void ToggleSelectionMode()
    {
        IsSelectionMode = !IsSelectionMode;
        if (!IsSelectionMode)
        {
            SelectedReceipts.Clear();
        }
    }

    [RelayCommand]
    private async Task DeleteSelectedAsync()
    {
        if (!SelectedReceipts.Any())
        {
            await Shell.Current.DisplayAlert("No Selection", "Please select receipts to delete", "OK");
            return;
        }

        var count = SelectedReceipts.Count;
        bool confirm = await Shell.Current.DisplayAlert(
            "Delete Receipts",
            $"Are you sure you want to delete {count} receipt(s)?",
            "Yes, Delete",
            "Cancel");

        if (!confirm)
            return;

        try
        {
            IsBusy = true;

            foreach (var receipt in SelectedReceipts.ToList())
            {
                await _receiptRepository.DeleteAsync(receipt.Id);
                _allReceipts.Remove(receipt);
                Receipts.Remove(receipt);
            }

            SelectedReceipts.Clear();
            IsSelectionMode = false;

            await Shell.Current.DisplayAlert("Success", $"{count} receipt(s) deleted", "OK");
            UpdateStatistics();
        }
        catch (Exception ex)
        {
            SetError($"Failed to delete receipts: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task FilterByCategoryAsync(ReceiptCategory category)
    {
        SelectedCategory = category;
        await ApplyFiltersAsync();
    }

    [RelayCommand]
    private async Task FilterByDateRangeAsync(string range)
    {
        var today = DateTime.Today;

        switch (range)
        {
            case "Today":
                StartDate = today;
                EndDate = today;
                break;
            case "This Week":
                StartDate = today.AddDays(-(int)today.DayOfWeek);
                EndDate = today;
                break;
            case "This Month":
                StartDate = new DateTime(today.Year, today.Month, 1);
                EndDate = today;
                break;
            case "Last 30 Days":
                StartDate = today.AddDays(-30);
                EndDate = today;
                break;
            case "Last 90 Days":
                StartDate = today.AddDays(-90);
                EndDate = today;
                break;
            case "This Year":
                StartDate = new DateTime(today.Year, 1, 1);
                EndDate = today;
                break;
            default:
                StartDate = null;
                EndDate = null;
                break;
        }

        await ApplyFiltersAsync();
    }

    [RelayCommand]
    private async Task ChangeSortAsync()
    {
        var sortOptions = new[]
        {
            "Date (Newest)",
            "Date (Oldest)",
            "Amount (Highest)",
            "Amount (Lowest)",
            "Merchant (A-Z)",
            "Merchant (Z-A)"
        };

        var selected = await Shell.Current.DisplayActionSheet(
            "Sort By",
            "Cancel",
            null,
            sortOptions);

        if (selected != null && selected != "Cancel")
        {
            SortBy = selected;
            await ApplyFiltersAsync();
        }
    }

    // Add this command after your existing RelayCommands

    [RelayCommand]
    private async Task NavigateToCaptureAsync()
    {
        await Shell.Current.GoToAsync(nameof(CaptureReceiptPage));
    }

    //[RelayCommand]
    //private async Task NavigateToFilterPageAsync()
    //{
    //    // TODO: Implement when FilterPage is created
    //    await Shell.Current.GoToAsync(nameof(ReceiptFilterPage));

    //    // Alternative: Use DisplayActionSheet for quick filtering
    //    // await ShowFilterOptionsAsync();
    //}

    //// Optional: Quick filter popup instead of separate page
    //private async Task ShowFilterOptionsAsync()
    //{
    //    var categories = AvailableCategories.Select(c => c.Name).ToArray();

    //    var selectedCategory = await Shell.Current.DisplayActionSheet(
    //        "Filter by Category",
    //        "Cancel",
    //        "Clear Filter",
    //        categories);

    //    if (selectedCategory == "Clear Filter")
    //    {
    //        SelectedCategory = null;
    //        await ApplyFiltersAsync();
    //    }
    //    else if (selectedCategory != "Cancel" && selectedCategory != null)
    //    {
    //        var category = AvailableCategories.FirstOrDefault(c => c.Name == selectedCategory);
    //        if (category != null)
    //        {
    //            SelectedCategory = category.Type;
    //            await ApplyFiltersAsync();
    //        }
    //    }
    //}

    private void UpdateStatistics()
    {
        TotalReceiptCount = _allReceipts.Count;
        TotalAmount = _allReceipts.Sum(r => r.Total);
    }

    private void UpdateFilterStatus()
    {
        var activeFilters = new List<string>();

        if (!string.IsNullOrWhiteSpace(SearchText))
            activeFilters.Add($"Search: {SearchText}");

        if (SelectedCategory.HasValue)
            activeFilters.Add($"Category: {SelectedCategory.Value}");

        if (StartDate.HasValue || EndDate.HasValue)
            activeFilters.Add("Date Range");

        if (MinAmount.HasValue || MaxAmount.HasValue)
            activeFilters.Add("Amount Range");

        FilterStatus = activeFilters.Any()
            ? $"{Receipts.Count} of {_allReceipts.Count} receipts ({string.Join(", ", activeFilters)})"
            : $"All Receipts ({Receipts.Count})";
    }
}