namespace ReceiptWise.App.Views;

using ReceiptWise.App.ViewModels;

public partial class InsightsPage : ContentPage
{
    public InsightsPage(InsightsViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}