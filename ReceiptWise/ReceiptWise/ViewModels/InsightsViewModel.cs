namespace ReceiptWise.App.ViewModels;

using CommunityToolkit.Mvvm.Input;

/// <summary>
/// ViewModel for Insights page
/// </summary>
public partial class InsightsViewModel : BaseViewModel
{
    public InsightsViewModel()
    {
        Title = "Insights";
    }

    [RelayCommand]
    private async Task LoadInsightsAsync()
    {
        // Placeholder for Milestone 7
        await Task.CompletedTask;
    }
}