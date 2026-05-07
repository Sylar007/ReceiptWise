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
using ReceiptWise.Services.AI;
using ReceiptWise.Services.Business;
using ReceiptWise.Services.Configuration;
using SkiaSharp.Views.Maui.Controls.Hosting;
using ReceiptWise.App.Services;

namespace ReceiptWise.App;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .UseMauiCommunityToolkit()
            .UseSkiaSharp()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

#if DEBUG
        builder.Logging.AddDebug();
#endif

        // Initialize SQLitePCL
        SQLitePCL.Batteries_V2.Init();

        // Register Configuration
        var azureConfig = LoadAzureConfiguration();
        builder.Services.AddSingleton(azureConfig);

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
        builder.Services.AddSingleton<IReceiptExtractionService, AzureDocumentIntelligenceService>();

        // Register AI Services
        builder.Services.AddSingleton<CategoryMappingEngine>();
        builder.Services.AddSingleton<AzureOpenAIService>();
        builder.Services.AddSingleton<ICategorySuggestionService, CategorySuggestionService>();

        // Register Insights Service
        builder.Services.AddSingleton<InsightsService>();

        // Register Export and Backup Services
        builder.Services.AddSingleton<ExportService>();
        builder.Services.AddSingleton<BackupService>();

        // Register Business Services
        builder.Services.AddSingleton<ReceiptProcessingService>();

        // Register Notification Service
        builder.Services.AddSingleton<INotificationService, NotificationService>();

        // Register Optimization and Security Services
        builder.Services.AddSingleton<ImageOptimizationService>();
        builder.Services.AddSingleton<SecureStorageService>();
        builder.Services.AddSingleton<ErrorHandlingService>();
        builder.Services.AddSingleton<PerformanceMonitoringService>();

        // Register ViewModels
        builder.Services.AddTransient<WarrantyViewModel>();

        // Register Views
        builder.Services.AddTransient<WarrantyPage>();

        // Register Helpers
        builder.Services.AddSingleton<ImageHelper>();

        // Register Seeder
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

        // REMOVE THIS LINE - IT'S BLOCKING THE UI THREAD:
        // InitializeDatabaseAsync(app.Services).Wait();

        // Instead, initialize database in background
        Task.Run(async () =>
        {
            try
            {
                await InitializeDatabaseAsync(app.Services);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Database initialization failed: {ex.Message}");
            }
        });

        return app;
    }

    private static AzureAIConfiguration LoadAzureConfiguration()
    {
        // Try to load from Preferences first (for backward compatibility)
        // In production, use SecureStorageService

        var config = new AzureAIConfiguration
        {
            DocumentIntelligence = new DocumentIntelligenceSettings
            {
                Endpoint = Preferences.Get("AzureDocIntelligence_Endpoint", string.Empty),
                ApiKey = Preferences.Get("AzureDocIntelligence_ApiKey", string.Empty),
                TimeoutSeconds = 30,
                MaxRetries = 3
            },
            OpenAI = new OpenAISettings
            {
                Endpoint = Preferences.Get("AzureOpenAI_Endpoint", string.Empty),
                ApiKey = Preferences.Get("AzureOpenAI_ApiKey", string.Empty),
                DeploymentName = Preferences.Get("AzureOpenAI_Deployment", "gpt-4"),
                MaxTokens = 150,
                Temperature = 0.1f
            }
        };

        return config;
    }

    private static async Task InitializeDatabaseAsync(IServiceProvider services)
    {
        try
        {
            System.Diagnostics.Debug.WriteLine("\n╔══════════════════════════════════════════════════════════╗");
            System.Diagnostics.Debug.WriteLine("║       Database Initialization Started                    ║");
            System.Diagnostics.Debug.WriteLine("╚══════════════════════════════════════════════════════════╝");
            
            System.Diagnostics.Debug.WriteLine("→ Resolving ReceiptWiseDatabase...");
            var database = services.GetRequiredService<ReceiptWiseDatabase>();
            System.Diagnostics.Debug.WriteLine("✓ Database service resolved");
            
            System.Diagnostics.Debug.WriteLine("→ Initializing database...");
            await database.InitializeAsync();
            System.Diagnostics.Debug.WriteLine("✓ Database initialized");

            System.Diagnostics.Debug.WriteLine("→ Resolving ICategoryRepository...");
            var categoryRepo = services.GetRequiredService<ICategoryRepository>();
            System.Diagnostics.Debug.WriteLine("✓ Category repository resolved");
            
            System.Diagnostics.Debug.WriteLine("→ Initializing default categories...");
            await categoryRepo.InitializeDefaultCategoriesAsync();
            System.Diagnostics.Debug.WriteLine("✓ Default categories initialized");

#if DEBUG
            System.Diagnostics.Debug.WriteLine("→ Resolving SampleDataSeeder...");
            var seeder = services.GetRequiredService<SampleDataSeeder>();
            System.Diagnostics.Debug.WriteLine("✓ Seeder resolved");
            
            System.Diagnostics.Debug.WriteLine("→ Resolving IReceiptRepository...");
            var receiptRepo = services.GetRequiredService<IReceiptRepository>();
            System.Diagnostics.Debug.WriteLine("✓ Receipt repository resolved");
            
            System.Diagnostics.Debug.WriteLine("→ Getting receipt count...");
            var count = await receiptRepo.GetCountAsync();
            System.Diagnostics.Debug.WriteLine($"✓ Existing receipt count: {count}");

            if (count == 0)
            {
                System.Diagnostics.Debug.WriteLine("→ Seeding sample receipts (15)...");
                await seeder.SeedSampleReceiptsAsync(15);
                var logger = services.GetService<ILogger<ReceiptWiseDatabase>>();
                logger?.LogInformation("Seeded 15 sample receipts for development");
                System.Diagnostics.Debug.WriteLine("✓ Sample receipts seeded successfully");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("✓ Skipping seeding (data exists)");
            }
#endif
            System.Diagnostics.Debug.WriteLine("\n╔══════════════════════════════════════════════════════════╗");
            System.Diagnostics.Debug.WriteLine("║    Database Initialization Complete ✓                    ║");
            System.Diagnostics.Debug.WriteLine("╚══════════════════════════════════════════════════════════╝\n");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine("\n╔══════════════════════════════════════════════════════════╗");
            System.Diagnostics.Debug.WriteLine("║    DATABASE INITIALIZATION FAILED ✗                      ║");
            System.Diagnostics.Debug.WriteLine("╠══════════════════════════════════════════════════════════╣");
            System.Diagnostics.Debug.WriteLine($"║ Exception Type: {ex.GetType().FullName}");
            System.Diagnostics.Debug.WriteLine($"║ Message: {ex.Message}");
            System.Diagnostics.Debug.WriteLine("╠══════════════════════════════════════════════════════════╣");
            System.Diagnostics.Debug.WriteLine("║ Stack Trace:");
            System.Diagnostics.Debug.WriteLine(ex.StackTrace);
            
            if (ex.InnerException != null)
            {
                System.Diagnostics.Debug.WriteLine("╠══════════════════════════════════════════════════════════╣");
                System.Diagnostics.Debug.WriteLine($"║ Inner Exception: {ex.InnerException.GetType().FullName}");
                System.Diagnostics.Debug.WriteLine($"║ Message: {ex.InnerException.Message}");
                System.Diagnostics.Debug.WriteLine("║ Stack Trace:");
                System.Diagnostics.Debug.WriteLine(ex.InnerException.StackTrace);
            }
            
            System.Diagnostics.Debug.WriteLine("╚══════════════════════════════════════════════════════════╝\n");
            
            var logger = services.GetService<ILogger<ReceiptWiseDatabase>>();
            logger?.LogError(ex, "Failed to initialize database");
            throw;
        }
    }
}