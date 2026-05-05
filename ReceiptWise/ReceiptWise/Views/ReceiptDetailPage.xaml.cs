namespace ReceiptWise.App.Views;

using ReceiptWise.App.ViewModels;

public partial class ReceiptDetailPage : ContentPage
{
    public ReceiptDetailPage(ReceiptDetailViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}