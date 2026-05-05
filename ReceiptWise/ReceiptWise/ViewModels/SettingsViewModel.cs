namespace ReceiptWise.App.ViewModels;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

/// <summary>
/// ViewModel for Settings page
/// </summary>
public partial class SettingsViewModel : BaseViewModel
{
    [ObservableProperty]
    private bool _aiEnabled = true;

    [ObservableProperty]
    private string _appVersion = "1.0.0";

    public SettingsViewModel()
    {
        Title = "Settings";
    }

    [RelayCommand]
    private async Task ExportDataAsync()
    {
        // Placeholder for Milestone 8
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