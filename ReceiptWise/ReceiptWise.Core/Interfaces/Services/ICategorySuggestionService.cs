namespace ReceiptWise.Core.Interfaces.Services;

using ReceiptWise.Core.Enums;
using ReceiptWise.Core.Models.DTOs;

/// <summary>
/// Service for AI-powered category suggestions
/// </summary>
public interface ICategorySuggestionService
{
    /// <summary>
    /// Suggest category using rule-based engine
    /// </summary>
    ReceiptCategory SuggestCategoryByRules(string merchantName);

    /// <summary>
    /// Suggest category using Azure OpenAI (fallback)
    /// </summary>
    Task<CategorySuggestionDto> SuggestCategoryByAIAsync(
        string merchantName,
        IEnumerable<string> items,
        CancellationToken cancellationToken = default);
}