namespace ReceiptWise.App.ViewModels;

using CommunityToolkit.Mvvm.ComponentModel;

/// <summary>
/// Base ViewModel with common properties
/// </summary>
public partial class BaseViewModel : ObservableObject
{
    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private string _title = string.Empty;

    [ObservableProperty]
    private string _errorMessage = string.Empty;

    [ObservableProperty]
    private bool _hasError;

    public void ClearError()
    {
        ErrorMessage = string.Empty;
        HasError = false;
    }

    public void SetError(string message)
    {
        ErrorMessage = message;
        HasError = true;
    }
}