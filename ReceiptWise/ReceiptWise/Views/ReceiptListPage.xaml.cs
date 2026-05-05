namespace ReceiptWise.App.Views;

using ReceiptWise.App.ViewModels;

public partial class ReceiptListPage : ContentPage
{
    public ReceiptListPage(ReceiptListViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}