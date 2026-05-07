namespace ReceiptWise.App.Views;

using ReceiptWise.App.ViewModels;

public partial class WarrantyPage : ContentPage
{
    public WarrantyPage(WarrantyViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}