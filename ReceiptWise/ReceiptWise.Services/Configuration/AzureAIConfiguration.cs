namespace ReceiptWise.Services.Configuration;

/// <summary>
/// Configuration for Azure AI services
/// </summary>
public class AzureAIConfiguration
{
    public DocumentIntelligenceSettings DocumentIntelligence { get; set; } = new();
    public OpenAISettings OpenAI { get; set; } = new();
}

public class DocumentIntelligenceSettings
{
    public string Endpoint { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public int TimeoutSeconds { get; set; } = 30;
    public int MaxRetries { get; set; } = 3;
}

public class OpenAISettings
{
    public string Endpoint { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public string DeploymentName { get; set; } = string.Empty;
    public int MaxTokens { get; set; } = 150;
    public float Temperature { get; set; } = 0.1f;
}