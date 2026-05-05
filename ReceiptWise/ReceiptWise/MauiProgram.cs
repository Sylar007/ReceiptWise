using CommunityToolkit.Maui;
using Microsoft.Extensions.Logging;
using ReceiptWise.App.ViewModels;
using ReceiptWise.App.Views;
using ReceiptWise.Core.Interfaces.Repositories;
using ReceiptWise.Data.Context;
using ReceiptWise.Data.Repositories;
using SkiaSharp.Views.Maui.Controls.Hosting;
using Windows.UI.ApplicationSettings;

namespace ReceiptWise.App;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .UseMauiCommunityToolkit()
            .UseSkiaSharp() // For LiveCharts
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

#if DEBUG
        builder.Logging.AddDebug();
#endif

        // Register Database
        var dbPath = Path.Combine(FileSystem.AppDataDirectory, "receiptwise.db3");
        builder.Services.AddSingleton(new ReceiptWiseDatabase(dbPath));

        // Register Repositories
        builder.Services.AddSingleton<IReceiptRepository, ReceiptRepository>();
        builder.Services.AddSingleton<ICategoryRepository, CategoryRepository>();
        builder.Services.AddSingleton<IAttachmentRepository, AttachmentRepository>();

        // Register ViewModels
        builder.Services.AddTransient<HomeViewModel>();
        builder.Services.AddTransient<ReceiptListViewModel>();
        builder.Services.AddTransient<CaptureReceiptViewModel>();
        builder.Services.AddTransient<ReceiptDetailViewModel>();
        builder.Services.AddTransient<InsightsViewModel>();
        builder.Services.AddTransient<SettingsViewModel>();

        // Register Views
        builder.Services.AddTransient<HomePage>();
        builder.Services.AddTransient<ReceiptListPage>();
        builder.Services.AddTransient<CaptureReceiptPage>();
        builder.Services.AddTransient<ReceiptDetailPage>();
        builder.Services.AddTransient<InsightsPage>();
        builder.Services.AddTransient<SettingsPage>();

        // Initialize categories on startup
        var app = builder.Build();
        InitializeDatabaseAsync(app.Services).Wait();

        return app;
    }

    private static async Task InitializeDatabaseAsync(IServiceProvider services)
    {
        var categoryRepo = services.GetRequiredService<ICategoryRepository>();
        await categoryRepo.InitializeDefaultCategoriesAsync();
    }
}