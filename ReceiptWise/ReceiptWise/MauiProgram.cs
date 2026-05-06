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

        // Register Business Services
        builder.Services.AddSingleton<ReceiptProcessingService>();

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
        InitializeDatabaseAsync(app.Services).Wait();

        return app;
    }

    /// <summary>
    /// Load Azure AI configuration from secure storage or environment
    /// </summary>
    private static AzureAIConfiguration LoadAzureConfiguration()
    {
        // In production, load from SecureStorage or backend API
        // For development, use Preferences or environment variables

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
            var database = services.GetRequiredService<ReceiptWiseDatabase>();
            await database.InitializeAsync();

            var categoryRepo = services.GetRequiredService<ICategoryRepository>();
            await categoryRepo.InitializeDefaultCategoriesAsync();

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