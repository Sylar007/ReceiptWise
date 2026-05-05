namespace ReceiptWise.App.Views;

using ReceiptWise.App.ViewModels;

public partial class CaptureReceiptPage : ContentPage
{
    public CaptureReceiptPage(CaptureReceiptViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}