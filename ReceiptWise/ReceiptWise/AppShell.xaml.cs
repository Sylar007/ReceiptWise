namespace ReceiptWise.App;

using ReceiptWise.App.Views;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();

        // Register routes for navigation
        Routing.RegisterRoute(nameof(ReceiptDetailPage), typeof(ReceiptDetailPage));
        Routing.RegisterRoute(nameof(WarrantyPage), typeof(WarrantyPage));
        // Register navigation routes for pages not in TabBar
        Routing.RegisterRoute(nameof(CaptureReceiptPage), typeof(CaptureReceiptPage));
        Routing.RegisterRoute(nameof(ReceiptDetailPage), typeof(ReceiptDetailPage));
        //Routing.RegisterRoute(nameof(ReceiptFilterPage), typeof(ReceiptFilterPage));
    }
}