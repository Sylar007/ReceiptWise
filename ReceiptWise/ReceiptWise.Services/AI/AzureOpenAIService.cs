namespace ReceiptWise.Services.AI;

using Azure;
using Azure.AI.OpenAI;
using Microsoft.Extensions.Logging;
using ReceiptWise.Core.Enums;
using ReceiptWise.Core.Models.DTOs;
using ReceiptWise.Core.Exceptions;
using ReceiptWise.Services.Configuration;
using System.Text.Json;

/// <summary>
/// Service for AI-powered category suggestions using Azure OpenAI (GPT-4)
/// Used as fallback when rule-based matching fails
/// </summary>
public class AzureOpenAIService
{
    private readonly OpenAIClient? _client;
    private readonly OpenAISettings _settings;
    private readonly ILogger<AzureOpenAIService>? _logger;

    public AzureOpenAIService(
        AzureAIConfiguration configuration,
        ILogger<AzureOpenAIService>? logger = null)
    {
        _logger = logger;
        _settings = configuration.OpenAI;

        if (string.IsNullOrWhiteSpace(_settings.Endpoint) || string.IsNullOrWhiteSpace(_settings.ApiKey))
        {
            _logger?.LogWarning("Azure OpenAI is not configured. AI categorization will be disabled.");
            _client = null;
            return;
        }

        try
        {
            _client = new OpenAIClient(
                new Uri(_settings.Endpoint),
                new AzureKeyCredential(_settings.ApiKey));

            _logger?.LogInformation("Azure OpenAI client initialized successfully");
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to initialize Azure OpenAI client");
        }
    }

    /// <summary>
    /// Suggest category using GPT-4 based on merchant name and items
    /// </summary>
    public async Task<CategorySuggestionDto?> SuggestCategoryAsync(
        string merchantName,
        IEnumerable<string> itemDescriptions,
        CancellationToken cancellationToken = default)
    {
        if (_client == null)
        {
            _logger?.LogWarning("Azure OpenAI not configured, skipping AI categorization");
            return null;
        }

        try
        {
            _logger?.LogInformation("Requesting AI category suggestion for merchant: {Merchant}", merchantName);

            var prompt = BuildCategorizationPrompt(merchantName, itemDescriptions);

            var chatCompletionsOptions = new ChatCompletionsOptions
            {
                DeploymentName = _settings.DeploymentName,
                Messages =
                {
                    new ChatRequestSystemMessage(GetSystemPrompt()),
                    new ChatRequestUserMessage(prompt)
                },
                Temperature = _settings.Temperature,
                MaxTokens = _settings.MaxTokens,
                NucleusSamplingFactor = 0.1f // Low temperature for deterministic output
            };

            var response = await _client.GetChatCompletionsAsync(
                chatCompletionsOptions,
                cancellationToken);

            var result = response.Value;
            var content = result.Choices[0].Message.Content;

            _logger?.LogDebug("AI categorization response: {Response}", content);

            return ParseCategorizationResponse(content);
        }
        catch (RequestFailedException ex) when (ex.Status == 401)
        {
            _logger?.LogError(ex, "Azure OpenAI authentication failed");
            throw new ExtractionException("OpenAI authentication failed. Check your API key.", ex);
        }
        catch (RequestFailedException ex) when (ex.Status == 429)
        {
            _logger?.LogError(ex, "Azure OpenAI rate limit exceeded");
            return null; // Gracefully degrade, don't throw
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to get AI category suggestion");
            return null; // Gracefully degrade
        }
    }

    /// <summary>
    /// Build prompt for GPT-4 categorization
    /// </summary>
    private string BuildCategorizationPrompt(string merchantName, IEnumerable<string> items)
    {
        var itemsList = items.Any()
            ? string.Join(", ", items.Take(10)) // Limit to 10 items to save tokens
            : "No items available";

        return $@"Categorize this receipt:

Merchant: {merchantName}
Items: {itemsList}

Available categories:
- Groceries: Food and household items
- Dining: Restaurants, cafes, fast food
- Transportation: Gas, parking, ride-sharing
- Shopping: Retail stores, clothing, general merchandise
- Healthcare: Pharmacies, medical services
- Utilities: Electric, water, internet, phone
- Entertainment: Movies, games, streaming services
- Travel: Hotels, flights, car rentals
- HomeAndGarden: Home improvement, furniture, garden
- Technology: Electronics, computers, software
- Services: Professional services, personal care
- Other: Anything that doesn't fit above

Return ONLY a JSON object with this exact format:
{{
  ""category"": ""CategoryName"",
  ""confidence"": 0.95,
  ""reasoning"": ""Brief explanation""
}}

Example:
{{
  ""category"": ""Groceries"",
  ""confidence"": 0.92,
  ""reasoning"": ""Walmart sells primarily groceries and household items""
}}";
    }

    /// <summary>
    /// System prompt to guide GPT-4 behavior
    /// </summary>
    private string GetSystemPrompt()
    {
        return @"You are a receipt categorization expert. Your job is to accurately categorize receipts into one of the predefined categories based on the merchant name and purchased items.

Rules:
1. Always return valid JSON
2. Choose the MOST SPECIFIC category that fits
3. Confidence should be 0.7-1.0 (use 0.7 if uncertain)
4. Keep reasoning brief (one sentence)
5. If truly unsure, use 'Other' category
6. Be consistent: same merchant = same category";
    }

    /// <summary>
    /// Parse GPT-4 JSON response into DTO
    /// </summary>
    private CategorySuggestionDto? ParseCategorizationResponse(string jsonResponse)
    {
        try
        {
            // Clean response (GPT sometimes adds markdown)
            var cleaned = jsonResponse.Trim();
            if (cleaned.StartsWith("```json"))
            {
                cleaned = cleaned.Substring(7);
            }
            if (cleaned.EndsWith("```"))
            {
                cleaned = cleaned.Substring(0, cleaned.Length - 3);
            }
            cleaned = cleaned.Trim();

            var response = JsonSerializer.Deserialize<AICategoryResponse>(cleaned, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (response == null)
            {
                _logger?.LogWarning("Failed to parse AI response as JSON");
                return null;
            }

            // Parse category string to enum
            if (!Enum.TryParse<ReceiptCategory>(response.Category, true, out var category))
            {
                _logger?.LogWarning("Unknown category returned: {Category}", response.Category);
                return null;
            }

            return new CategorySuggestionDto
            {
                SuggestedCategory = category,
                Confidence = response.Confidence,
                Reasoning = response.Reasoning
            };
        }
        catch (JsonException ex)
        {
            _logger?.LogError(ex, "Failed to parse AI categorization response: {Response}", jsonResponse);
            return null;
        }
    }

    // Helper class for JSON deserialization
    private class AICategoryResponse
    {
        public string Category { get; set; } = string.Empty;
        public float Confidence { get; set; }
        public string Reasoning { get; set; } = string.Empty;
    }
}