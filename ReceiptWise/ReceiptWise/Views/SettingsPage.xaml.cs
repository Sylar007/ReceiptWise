namespace ReceiptWise.App.Views;

using ReceiptWise.App.ViewModels;

public partial class SettingsPage : ContentPage
{
    public SettingsPage(SettingsViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}