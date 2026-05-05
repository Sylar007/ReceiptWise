namespace ReceiptWise.App;

using ReceiptWise.App.Views;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();

        // Register routes for navigation
        Routing.RegisterRoute(nameof(ReceiptDetailPage), typeof(ReceiptDetailPage));
    }
}