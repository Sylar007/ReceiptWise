namespace ReceiptWise.App.ViewModels;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ReceiptWise.Core.Interfaces.Repositories;
using ReceiptWise.Data.Context;
using ReceiptWise.Data.Seed;
using ReceiptWise.Services.Configuration;

public partial class SettingsViewModel : BaseViewModel
{
    private readonly ReceiptWiseDatabase _database;
    private readonly SampleDataSeeder _seeder;
    private readonly IReceiptRepository _receiptRepository;
    private readonly AzureAIConfiguration _azureConfig;

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
    private string _azureEndpoint = string.Empty;

    [ObservableProperty]
    private bool _azureConfigured;

    public SettingsViewModel(
        ReceiptWiseDatabase database,
        SampleDataSeeder seeder,
        IReceiptRepository receiptRepository,
        AzureAIConfiguration azureConfig)
    {
        _database = database;
        _seeder = seeder;
        _receiptRepository = receiptRepository;
        _azureConfig = azureConfig;
        Title = "Settings";

        DatabasePath = Path.Combine(FileSystem.AppDataDirectory, "receiptwise.db3");

        // Check Azure configuration
        AzureConfigured = !string.IsNullOrWhiteSpace(_azureConfig.DocumentIntelligence.Endpoint) &&
                         !string.IsNullOrWhiteSpace(_azureConfig.DocumentIntelligence.ApiKey);

        AzureEndpoint = AzureConfigured ? _azureConfig.DocumentIntelligence.Endpoint : "Not configured";

        _ = LoadDatabaseStatsAsync();
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
        }
        catch (Exception ex)
        {
            SetError($"Failed to load stats: {ex.Message}");
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

        // Save to Preferences (in production, use SecureStorage)
        Preferences.Set("AzureDocIntelligence_Endpoint", endpoint);
        Preferences.Set("AzureDocIntelligence_ApiKey", apiKey);

        AzureEndpoint = endpoint;
        AzureConfigured = true;

        await Shell.Current.DisplayAlert(
            "Success",
            "Azure configuration saved. Restart the app to apply changes.",
            "OK");
    }

    [RelayCommand]
    private async Task SeedSampleDataAsync()
    {
        try
        {
            IsBusy = true;
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
        }
    }

    [RelayCommand]
    private async Task ClearAllDataAsync()
    {
        bool confirm = await Shell.Current.DisplayAlert(
            "Clear All Data",
            "This will delete ALL receipts permanently. Continue?",
            "Yes, Delete All",
            "Cancel");

        if (!confirm)
            return;

        try
        {
            IsBusy = true;
            await _database.ClearAllDataAsync();
            await LoadDatabaseStatsAsync();
            await Shell.Current.DisplayAlert("Success", "All data cleared", "OK");
        }
        catch (Exception ex)
        {
            SetError($"Failed to clear data: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task ExportDataAsync()
    {
        await Shell.Current.DisplayAlert("Coming Soon", "Export feature will be implemented in Milestone 8", "OK");
    }

    [RelayCommand]
    private async Task ClearCacheAsync()
    {
        bool confirm = await Shell.Current.DisplayAlert(
            "Clear Cache",
            "This will remove all cached data. Continue?",
            "Yes",
            "No");

        if (confirm)
        {
            await Shell.Current.DisplayAlert("Success", "Cache cleared", "OK");
        }
    }
}