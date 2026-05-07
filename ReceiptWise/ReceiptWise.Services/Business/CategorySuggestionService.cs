namespace ReceiptWise.Services.Business;

using Microsoft.Extensions.Logging;
using ReceiptWise.Core.Interfaces.Services;
using ReceiptWise.Core.Enums;
using ReceiptWise.Core.Models.DTOs;
using ReceiptWise.Services.AI;

/// <summary>
/// Orchestrates category suggestions using rule-based engine + AI fallback
/// Implements caching for performance
/// </summary>
public class CategorySuggestionService : ICategorySuggestionService
{
    private readonly CategoryMappingEngine _mappingEngine;
    private readonly AzureOpenAIService _openAIService;
    private readonly ILogger<CategorySuggestionService>? _logger;

    // Simple in-memory cache (merchant -> category)
    private readonly Dictionary<string, ReceiptCategory> _categoryCache;
    private const int MaxCacheSize = 500;

    public CategorySuggestionService(
        CategoryMappingEngine mappingEngine,
        AzureOpenAIService openAIService,
        ILogger<CategorySuggestionService>? logger = null)
    {
        _mappingEngine = mappingEngine;
        _openAIService = openAIService;
        _logger = logger;
        _categoryCache = new Dictionary<string, ReceiptCategory>(StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Suggest category using rule-based engine (fast, offline)
    /// </summary>
    public ReceiptCategory SuggestCategoryByRules(string merchantName)
    {
        if (string.IsNullOrWhiteSpace(merchantName))
            return ReceiptCategory.Other;

        // Check cache first
        if (_categoryCache.TryGetValue(merchantName, out var cachedCategory))
        {
            _logger?.LogDebug("Cache hit for merchant: {Merchant} -> {Category}", merchantName, cachedCategory);
            return cachedCategory;
        }

        // Try rule-based matching
        var category = _mappingEngine.SuggestCategory(merchantName);

        if (category.HasValue)
        {
            // Cache the result
            CacheCategory(merchantName, category.Value);
            return category.Value;
        }

        // No match found
        _logger?.LogDebug("No rule-based match for merchant: {Merchant}, returning Other", merchantName);
        return ReceiptCategory.Other;
    }

    /// <summary>
    /// Suggest category using Azure OpenAI (slower, requires internet)
    /// Falls back to rule-based if AI fails
    /// </summary>
    public async Task<CategorySuggestionDto> SuggestCategoryByAIAsync(
        string merchantName,
        IEnumerable<string> items,
        CancellationToken cancellationToken = default)
    {
        _logger?.LogInformation("Starting AI categorization for merchant: {Merchant}", merchantName);

        // First try rule-based (always faster)
        var ruleCategory = _mappingEngine.SuggestCategory(merchantName, items);

        if (ruleCategory.HasValue)
        {
            var confidence = _mappingEngine.GetMatchConfidence(merchantName, ruleCategory.Value);

            _logger?.LogInformation(
                "Rule-based match found: {Merchant} -> {Category} (confidence: {Confidence:P0})",
                merchantName, ruleCategory.Value, confidence);

            // If high confidence, skip AI call
            if (confidence >= 0.8f)
            {
                CacheCategory(merchantName, ruleCategory.Value);

                return new CategorySuggestionDto
                {
                    SuggestedCategory = ruleCategory.Value,
                    Confidence = confidence,
                    Reasoning = "Rule-based match with high confidence"
                };
            }
        }

        // Try AI suggestion
        try
        {
            var aiSuggestion = await _openAIService.SuggestCategoryAsync(
                merchantName,
                items,
                cancellationToken);

            if (aiSuggestion != null)
            {
                _logger?.LogInformation(
                    "AI suggestion: {Merchant} -> {Category} (confidence: {Confidence:P0})",
                    merchantName, aiSuggestion.SuggestedCategory, aiSuggestion.Confidence);

                // Cache if confident
                if (aiSuggestion.Confidence >= 0.7f)
                {
                    CacheCategory(merchantName, aiSuggestion.SuggestedCategory);
                }

                return aiSuggestion;
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "AI categorization failed, falling back to rule-based");
        }

        // Fallback: use rule-based result or Other
        var fallbackCategory = ruleCategory ?? ReceiptCategory.Other;
        return new CategorySuggestionDto
        {
            SuggestedCategory = fallbackCategory,
            Confidence = ruleCategory.HasValue ? 0.6f : 0.3f,
            Reasoning = ruleCategory.HasValue
                ? "Rule-based match (medium confidence)"
                : "No confident match found, using Other"
        };
    }

    /// <summary>
    /// Cache category mapping with size limit
    /// </summary>
    private void CacheCategory(string merchantName, ReceiptCategory category)
    {
        if (_categoryCache.Count >= MaxCacheSize)
        {
            // Simple eviction: remove first item (FIFO)
            var firstKey = _categoryCache.Keys.First();
            _categoryCache.Remove(firstKey);
            _logger?.LogDebug("Cache full, evicted: {Key}", firstKey);
        }

        _categoryCache[merchantName] = category;
        _logger?.LogDebug("Cached category: {Merchant} -> {Category}", merchantName, category);
    }

    /// <summary>
    /// Clear the category cache (for testing/admin)
    /// </summary>
    public void ClearCache()
    {
        _categoryCache.Clear();
        _logger?.LogInformation("Category cache cleared");
    }

    /// <summary>
    /// Get cache statistics
    /// </summary>
    public (int Count, int Capacity) GetCacheStats()
    {
        return (_categoryCache.Count, MaxCacheSize);
    }
}