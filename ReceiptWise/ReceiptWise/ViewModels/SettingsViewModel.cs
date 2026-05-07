namespace ReceiptWise.App.ViewModels;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ReceiptWise.App.Services;
using ReceiptWise.Core.Interfaces.Repositories;
using ReceiptWise.Core.Interfaces.Services;
using ReceiptWise.Data.Context;
using ReceiptWise.Data.Seed;
using ReceiptWise.Services.Configuration;
using ReceiptWise.Services.Infrastructure;
using System.Text;

/// <summary>
/// Enhanced ViewModel for Settings page with export and backup features
/// </summary>
public partial class SettingsViewModel : BaseViewModel
{
    private readonly ReceiptWiseDatabase _database;
    private readonly SampleDataSeeder _seeder;
    private readonly IReceiptRepository _receiptRepository;
    private readonly AzureAIConfiguration _azureConfig;
    private readonly ExportService _exportService;
    private readonly BackupService _backupService;
    private readonly IFileStorageService _fileStorageService;

    [ObservableProperty]
    private bool _aiEnabled = true;

    [ObservableProperty]
    private string _appVersion = "1.0.0";

    [ObservableProperty]
    private int _receiptCount;

    [ObservableProperty]
    private string _databasePath = string.Empty;

    [ObservableProperty]
    private long _databaseSizeKB;

    [ObservableProperty]
    private long _attachmentsSizeKB;

    [ObservableProperty]
    private string _azureEndpoint = string.Empty;

    [ObservableProperty]
    private bool _azureConfigured;

    [ObservableProperty]
    private string _selectedCurrency = "USD";

    [ObservableProperty]
    private string _selectedLanguage = "English";

    private readonly SecureStorageService _secureStorageService;
       

    public SettingsViewModel(
        ReceiptWiseDatabase database,
        SampleDataSeeder seeder,
        IReceiptRepository receiptRepository,
        AzureAIConfiguration azureConfig,
        ExportService exportService,
        BackupService backupService,
        IFileStorageService fileStorageService,
        SecureStorageService secureStorageService)
    {
        _database = database;
        _seeder = seeder;
        _receiptRepository = receiptRepository;
        _azureConfig = azureConfig;
        _exportService = exportService;
        _backupService = backupService;
        _fileStorageService = fileStorageService;
        _secureStorageService = secureStorageService; // Add this
        Title = "Settings";

        DatabasePath = Path.Combine(FileSystem.AppDataDirectory, "receiptwise.db3");

        // Check Azure configuration
        AzureConfigured = !string.IsNullOrWhiteSpace(_azureConfig.DocumentIntelligence.Endpoint) &&
                         !string.IsNullOrWhiteSpace(_azureConfig.DocumentIntelligence.ApiKey);

        AzureEndpoint = AzureConfigured ? _azureConfig.DocumentIntelligence.Endpoint : "Not configured";

        // Load preferences
        SelectedCurrency = Preferences.Get("Currency", "USD");
        SelectedLanguage = Preferences.Get("Language", "English");

        _ = LoadDatabaseStatsAsync();
    }

    [RelayCommand]
    private async Task ClearAzureCredentialsAsync()
    {
        bool confirm = await Shell.Current.DisplayAlert(
            "Clear Azure Credentials",
            "This will remove your stored Azure API keys. You'll need to reconfigure them. Continue?",
            "Yes, Clear",
            "Cancel");

        if (!confirm)
            return;

        try
        {
            await _secureStorageService.ClearAllCredentialsAsync();

            AzureConfigured = false;
            AzureEndpoint = "Not configured";

            await Shell.Current.DisplayAlert("Success", "Azure credentials cleared", "OK");
        }
        catch (Exception ex)
        {
            SetError($"Failed to clear credentials: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task ConfigureAzureAsync()
    {
        var endpoint = await Shell.Current.DisplayPromptAsync(
            "Azure Document Intelligence",
            "Enter your Azure endpoint URL:",
            initialValue: _azureConfig.DocumentIntelligence.Endpoint,
            maxLength: 200,
            keyboard: Keyboard.Url);

        if (string.IsNullOrWhiteSpace(endpoint))
            return;

        var apiKey = await Shell.Current.DisplayPromptAsync(
            "Azure API Key",
            "Enter your API key:",
            maxLength: 100,
            keyboard: Keyboard.Text);

        if (string.IsNullOrWhiteSpace(apiKey))
            return;

        try
        {
            // Store using SecureStorage
            await _secureStorageService.SetAzureCredentialsAsync(endpoint, apiKey);

            AzureEndpoint = endpoint;
            AzureConfigured = true;

            await Shell.Current.DisplayAlert(
                "Success",
                "Azure configuration saved securely. Restart the app to apply changes.",
                "OK");
        }
        catch (Exception ex)
        {
            SetError($"Failed to save credentials: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task LoadDatabaseStatsAsync()
    {
        try
        {
            ReceiptCount = await _receiptRepository.GetCountAsync();

            if (File.Exists(DatabasePath))
            {
                var fileInfo = new FileInfo(DatabasePath);
                DatabaseSizeKB = fileInfo.Length / 1024;
            }

            // Calculate attachments size
            var storageSize = 0L;
            try
            {
                // Assuming all files are stored in a known directory, e.g., "attachments"
                var attachmentsDir = Path.Combine(FileSystem.AppDataDirectory, "attachments");
                if (Directory.Exists(attachmentsDir))
                {
                    var files = Directory.GetFiles(attachmentsDir, "*", SearchOption.AllDirectories);
                    foreach (var file in files)
                    {
                        var fileInfo = new FileInfo(file);
                        storageSize += fileInfo.Length;
                    }
                }
            }
            catch
            {
                // Optionally handle exceptions or log
            }
            AttachmentsSizeKB = storageSize / 1024;
        }
        catch (Exception ex)
        {
            SetError($"Failed to load stats: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task ExportAllReceiptsAsync()
    {
        try
        {
            IsBusy = true;
            StatusMessage = "Exporting receipts...";

            var receipts = await _receiptRepository.GetAllAsync();

            if (!receipts.Any())
            {
                await Shell.Current.DisplayAlert("No Data", "No receipts to export", "OK");
                return;
            }

            var exportType = await Shell.Current.DisplayActionSheet(
                "Export Format",
                "Cancel",
                null,
                "CSV (Simple)",
                "CSV (Detailed with Items)",
                "JSON (Backup)");

            if (exportType == "Cancel" || exportType == null)
                return;

            string filePath;

            if (exportType == "CSV (Simple)")
            {
                filePath = await _exportService.ExportToCsvAsync(receipts);
            }
            else if (exportType == "CSV (Detailed with Items)")
            {
                filePath = await _exportService.ExportDetailedCsvAsync(receipts);
            }
            else // JSON
            {
                filePath = await _exportService.ExportToJsonAsync(receipts);
            }

            await ShareFileAsync(filePath, $"Export {receipts.Count()} receipts");
        }
        catch (Exception ex)
        {
            SetError($"Failed to export: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
            StatusMessage = string.Empty;
        }
    }

    [RelayCommand]
    private async Task CreateBackupAsync()
    {
        try
        {
            IsBusy = true;
            StatusMessage = "Creating backup...";

            var backupPath = await _backupService.CreateBackupAsync();
            var backupInfo = await _backupService.GetBackupInfoAsync(backupPath);

            var message = backupInfo != null
                ? $"Backup created successfully!\n\nSize: {backupInfo.FileSize / 1024} KB\nFiles: {backupInfo.EntryCount}"
                : "Backup created successfully!";

            await Shell.Current.DisplayAlert("Backup Complete", message, "OK");
            await ShareFileAsync(backupPath, "ReceiptWise Backup");
        }
        catch (Exception ex)
        {
            SetError($"Failed to create backup: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
            StatusMessage = string.Empty;
        }
    }

    [RelayCommand]
    private async Task RestoreBackupAsync()
    {
        try
        {
            var result = await FilePicker.PickAsync(new PickOptions
            {
                PickerTitle = "Select Backup File",
                FileTypes = new FilePickerFileType(new Dictionary<DevicePlatform, IEnumerable<string>>
                {
                    { DevicePlatform.iOS, new[] { "public.zip-archive" } },
                    { DevicePlatform.Android, new[] { "application/zip" } },
                    { DevicePlatform.WinUI, new[] { ".zip" } },
                    { DevicePlatform.MacCatalyst, new[] { "public.zip-archive" } }
                })
            });

            if (result == null)
                return;

            bool confirm = await Shell.Current.DisplayAlert(
                "Restore Backup",
                "This will replace all current data with the backup. Your current data will be backed up first. Continue?",
                "Yes, Restore",
                "Cancel");

            if (!confirm)
                return;

            IsBusy = true;
            StatusMessage = "Restoring backup...";

            await _backupService.RestoreBackupAsync(result.FullPath);

            await Shell.Current.DisplayAlert(
                "Restore Complete",
                "Backup restored successfully! Please restart the app for changes to take effect.",
                "OK");
        }
        catch (Exception ex)
        {
            SetError($"Failed to restore backup: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
            StatusMessage = string.Empty;
        }
    }
    
    [RelayCommand]
    private async Task ChangeCurrencyAsync()
    {
        var currencies = new[] { "USD", "EUR", "GBP", "CAD", "AUD", "JPY", "CNY", "INR" };

        var selected = await Shell.Current.DisplayActionSheet(
            "Select Currency",
            "Cancel",
            null,
            currencies);

        if (selected != null && selected != "Cancel")
        {
            SelectedCurrency = selected;
            Preferences.Set("Currency", selected);
            await Shell.Current.DisplayAlert("Success", $"Currency changed to {selected}", "OK");
        }
    }

    [RelayCommand]
    private async Task ChangeLanguageAsync()
    {
        var languages = new[] { "English", "Spanish", "French", "German", "Chinese", "Japanese" };

        var selected = await Shell.Current.DisplayActionSheet(
            "Select Language",
            "Cancel",
            null,
            languages);

        if (selected != null && selected != "Cancel")
        {
            SelectedLanguage = selected;
            Preferences.Set("Language", selected);
            await Shell.Current.DisplayAlert("Success", $"Language changed to {selected}. Restart app to apply.", "OK");
        }
    }

    [RelayCommand]
    private async Task ClearAllDataAsync()
    {
        bool confirm = await Shell.Current.DisplayAlert(
            "Clear All Data",
            "This will delete ALL receipts and attachments permanently. This action cannot be undone. Continue?",
            "Yes, Delete Everything",
            "Cancel");

        if (!confirm)
            return;

        // Double confirmation
        bool doubleConfirm = await Shell.Current.DisplayAlert(
            "Are You Sure?",
            "This is your last chance to cancel. All data will be permanently deleted.",
            "Delete All Data",
            "Cancel");

        if (!doubleConfirm)
            return;

        try
        {
            IsBusy = true;
            StatusMessage = "Deleting all data...";

            await _database.ClearAllDataAsync();
            await _fileStorageService.ClearAllFilesAsync();

            await LoadDatabaseStatsAsync();

            await Shell.Current.DisplayAlert("Success", "All data has been deleted", "OK");
        }
        catch (Exception ex)
        {
            SetError($"Failed to clear data: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
            StatusMessage = string.Empty;
        }
    }

    [RelayCommand]
    private async Task ViewDiagnosticsAsync()
    {
        var diagnostics = new StringBuilder();
        diagnostics.AppendLine("=== ReceiptWise Diagnostics ===");
        diagnostics.AppendLine();
        diagnostics.AppendLine($"App Version: {AppVersion}");
        diagnostics.AppendLine($"Platform: {DeviceInfo.Platform}");
        diagnostics.AppendLine($"OS Version: {DeviceInfo.VersionString}");
        diagnostics.AppendLine($"Device: {DeviceInfo.Model}");
        diagnostics.AppendLine();
        diagnostics.AppendLine($"Database Path: {DatabasePath}");
        diagnostics.AppendLine($"Database Size: {DatabaseSizeKB} KB");
        diagnostics.AppendLine($"Attachments Size: {AttachmentsSizeKB} KB");
        diagnostics.AppendLine($"Total Receipts: {ReceiptCount}");
        diagnostics.AppendLine();
        diagnostics.AppendLine($"Azure AI Configured: {AzureConfigured}");
        diagnostics.AppendLine($"Azure Endpoint: {AzureEndpoint}");
        diagnostics.AppendLine();
        diagnostics.AppendLine($"Currency: {SelectedCurrency}");
        diagnostics.AppendLine($"Language: {SelectedLanguage}");

        await Shell.Current.DisplayAlert("Diagnostics", diagnostics.ToString(), "Close");
    }

    [RelayCommand]
    private async Task SeedSampleDataAsync()
    {
        try
        {
            IsBusy = true;
            StatusMessage = "Adding sample data...";

            await _seeder.SeedSampleReceiptsAsync(20);
            await LoadDatabaseStatsAsync();

            await Shell.Current.DisplayAlert("Success", "20 sample receipts added", "OK");
        }
        catch (Exception ex)
        {
            SetError($"Failed to seed data: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
            StatusMessage = string.Empty;
        }
    }

    private async Task ShareFileAsync(string filePath, string title)
    {
        await Share.RequestAsync(new ShareFileRequest
        {
            Title = title,
            File = new ShareFile(filePath)
        });
    }

    [ObservableProperty]
    private string _statusMessage = string.Empty;
}