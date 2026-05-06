using Microsoft.Extensions.Logging;
using CommunityToolkit.Maui;
using ReceiptWise.App.Views;
using ReceiptWise.App.ViewModels;
using ReceiptWise.Core.Interfaces.Repositories;
using ReceiptWise.Core.Interfaces.Services;
using ReceiptWise.Data.Context;
using ReceiptWise.Data.Repositories;
using ReceiptWise.Data.Seed;
using ReceiptWise.Services.Infrastructure;
using ReceiptWise.Services.Helpers;
using SkiaSharp.Views.Maui.Controls.Hosting;

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
        builder.Services.AddSingleton(sp =>
        {
            var logger = sp.GetService<ILogger<ReceiptWiseDatabase>>();
            return new ReceiptWiseDatabase(dbPath, logger);
        });

        // Register Repositories
        builder.Services.AddSingleton<IReceiptRepository, ReceiptRepository>();
        builder.Services.AddSingleton<ICategoryRepository, CategoryRepository>();
        builder.Services.AddSingleton<IAttachmentRepository, AttachmentRepository>();

        // Register Services
        builder.Services.AddSingleton<IFileStorageService, FileStorageService>();

        // Register Helpers
        builder.Services.AddSingleton<ImageHelper>();

        // Register Seeder (for development/testing)
        builder.Services.AddSingleton<SampleDataSeeder>();

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

        var app = builder.Build();

        // Initialize database on startup
        InitializeDatabaseAsync(app.Services).Wait();

        return app;
    }

    private static async Task InitializeDatabaseAsync(IServiceProvider services)
    {
        try
        {
            var database = services.GetRequiredService<ReceiptWiseDatabase>();
            await database.InitializeAsync();

            var categoryRepo = services.GetRequiredService<ICategoryRepository>();
            await categoryRepo.InitializeDefaultCategoriesAsync();

            // Seed sample data in DEBUG mode only
#if DEBUG
            var seeder = services.GetRequiredService<SampleDataSeeder>();
            var receiptRepo = services.GetRequiredService<IReceiptRepository>();
            var count = await receiptRepo.GetCountAsync();

            if (count == 0)
            {
                await seeder.SeedSampleReceiptsAsync(15);
                var logger = services.GetService<ILogger<ReceiptWiseDatabase>>();
                logger?.LogInformation("Seeded 15 sample receipts for development");
            }
#endif
        }
        catch (Exception ex)
        {
            var logger = services.GetService<ILogger<ReceiptWiseDatabase>>();
            logger?.LogError(ex, "Failed to initialize database");
            throw;
        }
    }
}