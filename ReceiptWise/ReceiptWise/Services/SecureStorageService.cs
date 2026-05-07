namespace ReceiptWise.App.Services;

using Microsoft.Extensions.Logging;

/// <summary>
/// Secure storage wrapper for sensitive data like API keys
/// Uses MAUI SecureStorage (platform-specific keychain/keystore)
/// </summary>
public class SecureStorageService
{
    private readonly ILogger<SecureStorageService>? _logger;

    public SecureStorageService(ILogger<SecureStorageService>? logger = null)
    {
        _logger = logger;
    }

    /// <summary>
    /// Store Azure AI credentials securely
    /// </summary>
    public async Task SetAzureCredentialsAsync(string endpoint, string apiKey)
    {
        try
        {
            await SecureStorage.SetAsync("AzureDocIntelligence_Endpoint", endpoint);
            await SecureStorage.SetAsync("AzureDocIntelligence_ApiKey", apiKey);

            _logger?.LogInformation("Azure credentials stored securely");
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to store Azure credentials in SecureStorage");

            // Fallback to Preferences if SecureStorage fails
            Preferences.Set("AzureDocIntelligence_Endpoint", endpoint);
            Preferences.Set("AzureDocIntelligence_ApiKey", apiKey);

            _logger?.LogWarning("Fell back to Preferences for credential storage");
        }
    }

    /// <summary>
    /// Retrieve Azure credentials
    /// </summary>
    public async Task<(string? endpoint, string? apiKey)> GetAzureCredentialsAsync()
    {
        try
        {
            var endpoint = await SecureStorage.GetAsync("AzureDocIntelligence_Endpoint");
            var apiKey = await SecureStorage.GetAsync("AzureDocIntelligence_ApiKey");

            // Fallback to Preferences if SecureStorage is empty
            if (string.IsNullOrEmpty(endpoint))
                endpoint = Preferences.Get("AzureDocIntelligence_Endpoint", string.Empty);

            if (string.IsNullOrEmpty(apiKey))
                apiKey = Preferences.Get("AzureDocIntelligence_ApiKey", string.Empty);

            return (endpoint, apiKey);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to retrieve Azure credentials from SecureStorage");

            // Fallback to Preferences
            var endpoint = Preferences.Get("AzureDocIntelligence_Endpoint", string.Empty);
            var apiKey = Preferences.Get("AzureDocIntelligence_ApiKey", string.Empty);

            return (endpoint, apiKey);
        }
    }

    /// <summary>
    /// Store Azure OpenAI credentials securely
    /// </summary>
    public async Task SetOpenAICredentialsAsync(string endpoint, string apiKey, string deploymentName)
    {
        try
        {
            await SecureStorage.SetAsync("AzureOpenAI_Endpoint", endpoint);
            await SecureStorage.SetAsync("AzureOpenAI_ApiKey", apiKey);
            await SecureStorage.SetAsync("AzureOpenAI_Deployment", deploymentName);

            _logger?.LogInformation("Azure OpenAI credentials stored securely");
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to store OpenAI credentials");

            // Fallback to Preferences
            Preferences.Set("AzureOpenAI_Endpoint", endpoint);
            Preferences.Set("AzureOpenAI_ApiKey", apiKey);
            Preferences.Set("AzureOpenAI_Deployment", deploymentName);
        }
    }

    /// <summary>
    /// Clear all stored credentials
    /// </summary>
    public async Task ClearAllCredentialsAsync()
    {
        try
        {
            // Clear from SecureStorage
            SecureStorage.Remove("AzureDocIntelligence_Endpoint");
            SecureStorage.Remove("AzureDocIntelligence_ApiKey");
            SecureStorage.Remove("AzureOpenAI_Endpoint");
            SecureStorage.Remove("AzureOpenAI_ApiKey");
            SecureStorage.Remove("AzureOpenAI_Deployment");

            // Also clear from Preferences (fallback storage)
            Preferences.Remove("AzureDocIntelligence_Endpoint");
            Preferences.Remove("AzureDocIntelligence_ApiKey");
            Preferences.Remove("AzureOpenAI_Endpoint");
            Preferences.Remove("AzureOpenAI_ApiKey");
            Preferences.Remove("AzureOpenAI_Deployment");

            _logger?.LogInformation("All credentials cleared");
            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to clear credentials");
        }
    }

    /// <summary>
    /// Check if Azure Document Intelligence is configured
    /// </summary>
    public async Task<bool> IsAzureConfiguredAsync()
    {
        var (endpoint, apiKey) = await GetAzureCredentialsAsync();
        return !string.IsNullOrWhiteSpace(endpoint) && !string.IsNullOrWhiteSpace(apiKey);
    }

    /// <summary>
    /// Check if Azure OpenAI is configured
    /// </summary>
    public async Task<bool> IsOpenAIConfiguredAsync()
    {
        try
        {
            var endpoint = await SecureStorage.GetAsync("AzureOpenAI_Endpoint");
            var apiKey = await SecureStorage.GetAsync("AzureOpenAI_ApiKey");

            if (string.IsNullOrEmpty(endpoint))
                endpoint = Preferences.Get("AzureOpenAI_Endpoint", string.Empty);

            if (string.IsNullOrEmpty(apiKey))
                apiKey = Preferences.Get("AzureOpenAI_ApiKey", string.Empty);

            return !string.IsNullOrWhiteSpace(endpoint) && !string.IsNullOrWhiteSpace(apiKey);
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Clear all app data (for testing/reset)
    /// </summary>
    public async Task ClearAllAppDataAsync()
    {
        try
        {
            SecureStorage.RemoveAll();
            Preferences.Clear();

            _logger?.LogWarning("All app data cleared");
            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to clear all app data");
        }
    }
}