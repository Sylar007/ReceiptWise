using Microsoft.Extensions.Logging;

namespace ReceiptWise.App;

public partial class App : Application
{
    public App()
    {
        try
        {
            InitializeComponent();

            // Add unhandled exception handlers
            AppDomain.CurrentDomain.UnhandledException += OnAppDomainUnhandledException;
            TaskScheduler.UnobservedTaskException += OnTaskSchedulerUnobservedTaskException;

            System.Diagnostics.Debug.WriteLine("===== App Initialization Started =====");
            MainPage = new AppShell();
            System.Diagnostics.Debug.WriteLine("===== AppShell Created Successfully =====");
        }
        catch (Exception ex)
        {
            LogException("App Constructor", ex);
            throw;
        }
    }

    private void OnAppDomainUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        if (e.ExceptionObject is Exception ex)
        {
            LogException("AppDomain.UnhandledException", ex);
        }
    }

    private void OnTaskSchedulerUnobservedTaskException(object sender, UnobservedTaskExceptionEventArgs e)
    {
        LogException("TaskScheduler.UnobservedTaskException", e.Exception);
        e.SetObserved();
    }

    private void LogException(string source, Exception ex)
    {
        System.Diagnostics.Debug.WriteLine($"\n╔════════════════════════════════════════════════════════");
        System.Diagnostics.Debug.WriteLine($"║ EXCEPTION in {source}");
        System.Diagnostics.Debug.WriteLine($"╠════════════════════════════════════════════════════════");
        System.Diagnostics.Debug.WriteLine($"║ Type: {ex.GetType().FullName}");
        System.Diagnostics.Debug.WriteLine($"║ Message: {ex.Message}");
        System.Diagnostics.Debug.WriteLine($"╠════════════════════════════════════════════════════════");
        System.Diagnostics.Debug.WriteLine($"║ Stack Trace:");
        System.Diagnostics.Debug.WriteLine(ex.StackTrace);
        
        if (ex.InnerException != null)
        {
            System.Diagnostics.Debug.WriteLine($"╠════════════════════════════════════════════════════════");
            System.Diagnostics.Debug.WriteLine($"║ Inner Exception: {ex.InnerException.GetType().FullName}");
            System.Diagnostics.Debug.WriteLine($"║ Message: {ex.InnerException.Message}");
            System.Diagnostics.Debug.WriteLine($"║ Stack Trace:");
            System.Diagnostics.Debug.WriteLine(ex.InnerException.StackTrace);
        }

        // For AggregateException, log all inner exceptions
        if (ex is AggregateException aggEx)
        {
            System.Diagnostics.Debug.WriteLine($"╠════════════════════════════════════════════════════════");
            System.Diagnostics.Debug.WriteLine($"║ AggregateException has {aggEx.InnerExceptions.Count} inner exception(s):");
            
            for (int i = 0; i < aggEx.InnerExceptions.Count; i++)
            {
                var innerEx = aggEx.InnerExceptions[i];
                System.Diagnostics.Debug.WriteLine($"║ [{i}] {innerEx.GetType().FullName}: {innerEx.Message}");
                System.Diagnostics.Debug.WriteLine($"║     Stack: {innerEx.StackTrace}");
            }
        }
        
        System.Diagnostics.Debug.WriteLine($"╚════════════════════════════════════════════════════════\n");
    }
}