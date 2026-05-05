namespace ReceiptWise.App.Views;

using ReceiptWise.App.ViewModels;
using CommunityToolkit.Maui.Behaviors;

public partial class HomePage : ContentPage
{
    public HomePage(HomeViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}