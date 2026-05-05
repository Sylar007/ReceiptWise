namespace ReceiptWise.App.ViewModels;

using CommunityToolkit.Mvvm.Input;

/// <summary>
/// ViewModel for Capture Receipt page
/// </summary>
public partial class CaptureReceiptViewModel : BaseViewModel
{
    public CaptureReceiptViewModel()
    {
        Title = "Capture Receipt";
    }

    [RelayCommand]
    private async Task TakePhotoAsync()
    {
        // Placeholder for Milestone 3
        await Shell.Current.DisplayAlert("Coming Soon", "Camera capture will be implemented in Milestone 3", "OK");
    }

    [RelayCommand]
    private async Task PickFileAsync()
    {
        // Placeholder for Milestone 3
        await Shell.Current.DisplayAlert("Coming Soon", "File picker will be implemented in Milestone 3", "OK");
    }
}