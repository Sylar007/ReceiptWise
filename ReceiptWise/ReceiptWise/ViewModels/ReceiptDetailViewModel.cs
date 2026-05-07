namespace ReceiptWise.App.ViewModels;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Polly.Caching;
using ReceiptWise.App.Views;
using ReceiptWise.Core.Enums;
using ReceiptWise.Core.Interfaces.Repositories;
using ReceiptWise.Core.Interfaces.Services;
using ReceiptWise.Core.Models.Domain;
using System.Collections.ObjectModel;
using static Azure.Core.HttpHeader;

/// <summary>
/// ViewModel for Receipt Detail page
/// Enhanced with full editing capabilities
/// </summary>
[QueryProperty(nameof(ReceiptId), "ReceiptId")]
public partial class ReceiptDetailViewModel : BaseViewModel
{
    private readonly IReceiptRepository _receiptRepository;
    private readonly ICategoryRepository _categoryRepository;
    private readonly IAttachmentRepository _attachmentRepository;
    private readonly IFileStorageService _fileStorageService;

    [ObservableProperty]
    private int _receiptId;

    [ObservableProperty]
    private Receipt? _receipt;

    [ObservableProperty]
    private ObservableCollection<Category> _availableCategories = new();

    [ObservableProperty]
    private ObservableCollection<ReceiptItem> _items = new();

    [ObservableProperty]
    private bool _isEditing;

    [ObservableProperty]
    private bool _hasAttachment;

    [ObservableProperty]
    private ImageSource? _attachmentImage;

    [ObservableProperty]
    private string? _attachmentPath;

    // Editable fields
    [ObservableProperty]
    private string _merchantName = string.Empty;

    [ObservableProperty]
    private DateTime _transactionDate = DateTime.Now;

    [ObservableProperty]
    private decimal _total;

    [ObservableProperty]
    private decimal _tax;

    [ObservableProperty]
    private decimal _subtotal;

    [ObservableProperty]
    private string _notes = string.Empty;

    [ObservableProperty]
    private ReceiptCategory _selectedCategory;

    public ReceiptDetailViewModel(
        IReceiptRepository receiptRepository,
        ICategoryRepository categoryRepository,
        IAttachmentRepository attachmentRepository,
        IFileStorageService fileStorageService)
    {
        _receiptRepository = receiptRepository;
        _categoryRepository = categoryRepository;
        _attachmentRepository = attachmentRepository;
        _fileStorageService = fileStorageService;
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
                return;
            }

            // Load editable fields
            MerchantName = Receipt.MerchantName;
            TransactionDate = Receipt.TransactionDate;
            Total = Receipt.Total;
            Tax = Receipt.Tax;
            Subtotal = Receipt.Subtotal;
            Notes = Receipt.Notes ?? string.Empty;
            SelectedCategory = Receipt.Category;

            // Load items
            Items.Clear();
            foreach (var item in Receipt.Items)
            {
                Items.Add(item);
            }

            // Load categories
            var categories = await _categoryRepository.GetAllAsync();
            AvailableCategories.Clear();
            foreach (var category in categories)
            {
                AvailableCategories.Add(category);
            }

            // Load attachment
            await LoadAttachmentAsync();
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
    private async Task LoadAttachmentAsync()
    {
        if (Receipt == null)
            return;

        try
        {
            var attachment = await _attachmentRepository.GetByReceiptIdAsync(Receipt.Id);

            if (attachment != null)
            {
                HasAttachment = true;
                AttachmentPath = attachment.FilePath;

                // Load image if it's an image file
                if (attachment.FileType.StartsWith("image/"))
                {
                    if (await _fileStorageService.FileExistsAsync(attachment.FilePath))
                    {
                        AttachmentImage = ImageSource.FromFile(attachment.FilePath);
                    }
                }
            }
            else
            {
                HasAttachment = false;
                AttachmentImage = null;
            }
        }
        catch (Exception ex)
        {
            // Log but don't fail - attachment is optional
            System.Diagnostics.Debug.WriteLine($"Failed to load attachment: {ex.Message}");
        }
    }

    [RelayCommand]
    private void ToggleEdit()
    {
        IsEditing = !IsEditing;

        if (!IsEditing)
        {
            // Reset to original values if cancelled
            if (Receipt != null)
            {
                MerchantName = Receipt.MerchantName;
                TransactionDate = Receipt.TransactionDate;
                Total = Receipt.Total;
                Tax = Receipt.Tax;
                Subtotal = Receipt.Subtotal;
                Notes = Receipt.Notes ?? string.Empty;
                SelectedCategory = Receipt.Category;
            }
        }
    }

    [RelayCommand]
    private async Task SaveChangesAsync()
    {
        if (Receipt == null)
            return;

        try
        {
            IsBusy = true;
            ClearError();

            // Validate
            if (string.IsNullOrWhiteSpace(MerchantName))
            {
                SetError("Merchant name is required");
                return;
            }

            if (Total < 0)
            {
                SetError("Total cannot be negative");
                return;
            }

            // Update receipt
            Receipt.MerchantName = MerchantName.Trim();
            Receipt.TransactionDate = TransactionDate;
            Receipt.Total = Total;
            Receipt.Tax = Tax;
            Receipt.Subtotal = Subtotal;
            Receipt.Notes = string.IsNullOrWhiteSpace(Notes) ? null : Notes.Trim();
            Receipt.Category = SelectedCategory;
            Receipt.ModifiedAt = DateTime.UtcNow;

            await _receiptRepository.UpdateAsync(Receipt);

            IsEditing = false;
            await Shell.Current.DisplayAlert("Success", "Receipt updated successfully", "OK");
        }
        catch (Exception ex)
        {
            SetError($"Failed to save changes: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task ChangeCategoryAsync()
    {
        if (Receipt == null)
            return;

        try
        {
            var categoryNames = AvailableCategories
                .Select(c => c.DisplayName)
                .ToArray();

            var selectedCategory = await Shell.Current.DisplayActionSheet(
                "Select Category",
                "Cancel",
                null,
                categoryNames);

            if (selectedCategory != null && selectedCategory != "Cancel")
            {
                var category = AvailableCategories.First(c => c.DisplayName == selectedCategory);
                SelectedCategory = category.CategoryType;

                if (!IsEditing)
                {
                    // Save immediately if not in edit mode
                    Receipt.Category = category.CategoryType;
                    await _receiptRepository.UpdateAsync(Receipt);
                    await Shell.Current.DisplayAlert("Success", $"Category changed to {selectedCategory}", "OK");
                }
            }
        }
        catch (Exception ex)
        {
            SetError($"Failed to update category: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task ViewAttachmentAsync()
    {
        if (!HasAttachment || string.IsNullOrEmpty(AttachmentPath))
            return;

        // Navigate to full-screen image viewer (implement in Milestone 10)
        await Shell.Current.DisplayAlert(
            "Attachment",
            $"Full-screen viewer coming soon.\n\nFile: {Path.GetFileName(AttachmentPath)}",
            "OK");
    }

    [RelayCommand]
    private async Task DeleteReceiptAsync()
    {
        if (Receipt == null)
            return;

        bool confirm = await Shell.Current.DisplayAlert(
            "Delete Receipt",
            "Are you sure you want to delete this receipt? This action cannot be undone.",
            "Yes, Delete",
            "Cancel");

        if (!confirm)
            return;

        try
        {
            IsBusy = true;

            // Delete attachment files if they exist
            if (HasAttachment && !string.IsNullOrEmpty(AttachmentPath))
            {
                await _fileStorageService.DeleteFileAsync(AttachmentPath);
            }

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

    [RelayCommand]
    private async Task AddItemAsync()
    {
        var description = await Shell.Current.DisplayPromptAsync(
            "Add Item",
            "Enter item description:",
            maxLength: 200);

        if (string.IsNullOrWhiteSpace(description))
            return;

        var priceStr = await Shell.Current.DisplayPromptAsync(
            "Add Item",
            "Enter item price:",
            keyboard: Keyboard.Numeric);

        if (string.IsNullOrWhiteSpace(priceStr) || !decimal.TryParse(priceStr, out var price))
            return;

        var item = new ReceiptItem
        {
            ReceiptId = ReceiptId,
            Description = description.Trim(),
            Quantity = 1,
            UnitPrice = price,
            TotalPrice = price
        };

        Items.Add(item);

        if (Receipt != null)
        {
            Receipt.Items.Add(item);
        }
    }

    [RelayCommand]
    private async Task RemoveItemAsync(ReceiptItem item)
    {
        if (item == null)
            return;

        bool confirm = await Shell.Current.DisplayAlert(
            "Remove Item",
            $"Remove '{item.Description}'?",
            "Yes",
            "No");

        if (!confirm)
            return;

        Items.Remove(item);

        if (Receipt != null)
        {
            Receipt.Items.Remove(item);
        }
    }

    [RelayCommand]
    private void CalculateSubtotal()
    {
        // Auto-calculate subtotal when total or tax changes
        if (Total >= Tax)
        {
            Subtotal = Total - Tax;
        }
    }

    // Add to ReceiptDetailViewModel class

    [RelayCommand]
    private async Task ExportReceiptAsync()
    {
        if (Receipt == null)
            return;

        try
        {
            IsBusy = true;

            var csv = GenerateReceiptCsv(Receipt);
            var fileName = $"Receipt_{Receipt.MerchantName}_{Receipt.TransactionDate:yyyyMMdd}.csv";
            var filePath = Path.Combine(FileSystem.CacheDirectory, fileName);

            await File.WriteAllTextAsync(filePath, csv);

            await Share.RequestAsync(new ShareFileRequest
            {
                Title = "Export Receipt",
                File = new ShareFile(filePath)
            });

            await Shell.Current.DisplayAlert("Success", "Receipt exported successfully", "OK");
        }
        catch (Exception ex)
        {
            SetError($"Failed to export receipt: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }
    // Add this command to existing ReceiptDetailViewModel

    [RelayCommand]
    private async Task ManageWarrantyAsync()
    {
        if (Receipt == null)
            return;

        await Shell.Current.GoToAsync($"{nameof(WarrantyPage)}?ReceiptId={Receipt.Id}");
    }
    private string GenerateReceiptCsv(Receipt receipt)
    {
        var sb = new System.Text.StringBuilder();

        // Header
        sb.AppendLine("Field,Value");
        sb.AppendLine($"Merchant,\"{receipt.MerchantName}\"");
        sb.AppendLine($"Date,{receipt.TransactionDate:yyyy-MM-dd}");
        sb.AppendLine($"Total,{receipt.Total:F2}");
        sb.AppendLine($"Tax,{receipt.Tax:F2}");
        sb.AppendLine($"Subtotal,{receipt.Subtotal:F2}");
        sb.AppendLine($"Currency,{receipt.Currency}");
        sb.AppendLine($"Category,{receipt.Category}");

        if (!string.IsNullOrWhiteSpace(receipt.Notes))
        {
            sb.AppendLine($"Notes,\"{receipt.Notes}\"");
        }

        // Line items
        if (receipt.Items.Any())
        {
            sb.AppendLine();
            sb.AppendLine("Item Description,Quantity,Unit Price,Total Price");
            foreach (var item in receipt.Items)
            {
                sb.AppendLine($"\"{item.Description}\",{item.Quantity},{item.UnitPrice:F2},{item.TotalPrice:F2}");
            }
        }

        return sb.ToString();
    }
}