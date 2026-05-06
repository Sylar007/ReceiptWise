namespace ReceiptWise.Services.AI;

using Azure;
using Azure.AI.FormRecognizer.DocumentAnalysis;
using Microsoft.Extensions.Logging;
using ReceiptWise.Core.Interfaces.Services;
using ReceiptWise.Core.Models.DTOs;
using ReceiptWise.Core.Enums;
using ReceiptWise.Core.Exceptions;
using ReceiptWise.Services.Configuration;
using System.Globalization;

/// <summary>
/// Service for extracting receipt data using Azure Document Intelligence
/// Uses the prebuilt-receipt model
/// </summary>
public class AzureDocumentIntelligenceService : IReceiptExtractionService
{
    private readonly DocumentAnalysisClient _client;
    private readonly ILogger<AzureDocumentIntelligenceService>? _logger;
    private readonly DocumentIntelligenceSettings _settings;

    public AzureDocumentIntelligenceService(
        AzureAIConfiguration configuration,
        ILogger<AzureDocumentIntelligenceService>? logger = null)
    {
        _logger = logger;
        _settings = configuration.DocumentIntelligence;

        if (string.IsNullOrWhiteSpace(_settings.Endpoint) || string.IsNullOrWhiteSpace(_settings.ApiKey))
        {
            _logger?.LogWarning("Azure Document Intelligence is not configured. Service will operate in offline mode.");
            _client = null!;
            return;
        }

        try
        {
            var credential = new AzureKeyCredential(_settings.ApiKey);
            _client = new DocumentAnalysisClient(new Uri(_settings.Endpoint), credential);
            _logger?.LogInformation("Azure Document Intelligence client initialized successfully");
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to initialize Azure Document Intelligence client");
            throw new ExtractionException("Failed to initialize extraction service", ex);
        }
    }

    /// <summary>
    /// Extract receipt data from image or PDF stream
    /// Supports: JPEG, PNG, BMP, TIFF, PDF
    /// </summary>
    public async Task<ExtractionResultDto> ExtractReceiptDataAsync(
        Stream fileStream,
        string contentType,
        CancellationToken cancellationToken = default)
    {
        if (fileStream == null || fileStream.Length == 0)
            throw new ArgumentException("File stream is null or empty", nameof(fileStream));

        if (_client == null)
        {
            _logger?.LogWarning("Azure Document Intelligence not configured, returning offline result");
            return CreateOfflineResult();
        }

        try
        {
            _logger?.LogInformation("Starting receipt extraction with Azure Document Intelligence");

            fileStream.Position = 0;

            // Use prebuilt-receipt model
            var operation = await _client.AnalyzeDocumentAsync(
                WaitUntil.Completed,
                "prebuilt-receipt",
                fileStream,
                cancellationToken: cancellationToken);

            _logger?.LogInformation("Extraction operation completed, processing results");

            var result = operation.Value;

            if (result.Pages.Count == 0)
            {
                _logger?.LogWarning("No pages detected in document");
                return new ExtractionResultDto
                {
                    Success = false,
                    ErrorMessage = "No pages detected in the document"
                };
            }

            // Extract receipt data from first page
            var extractedData = ParseReceiptResult(result);
            extractedData.Success = true;

            _logger?.LogInformation(
                "Receipt extraction successful: Merchant={Merchant}, Total={Total}, Items={ItemCount}",
                extractedData.MerchantName,
                extractedData.Total,
                extractedData.Items.Count);

            return extractedData;
        }
        catch (RequestFailedException ex) when (ex.Status == 401)
        {
            _logger?.LogError(ex, "Azure Document Intelligence authentication failed");
            throw new ExtractionException("Authentication failed. Check your API key.", ex);
        }
        catch (RequestFailedException ex) when (ex.Status == 429)
        {
            _logger?.LogError(ex, "Azure Document Intelligence rate limit exceeded");
            throw new ExtractionException("Rate limit exceeded. Please try again later.", ex);
        }
        catch (RequestFailedException ex)
        {
            _logger?.LogError(ex, "Azure Document Intelligence request failed with status {Status}", ex.Status);
            throw new ExtractionException($"Extraction failed: {ex.Message}", ex);
        }
        catch (OperationCanceledException)
        {
            _logger?.LogWarning("Receipt extraction was cancelled");
            throw;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Unexpected error during receipt extraction");
            throw new ExtractionException("An unexpected error occurred during extraction", ex);
        }
    }

    /// <summary>
    /// Parse Azure Document Intelligence result into our DTO
    /// </summary>
    private ExtractionResultDto ParseReceiptResult(AnalyzeResult result)
    {
        var dto = new ExtractionResultDto
        {
            Currency = CurrencyCode.USD,
            Items = new List<ReceiptItemDto>()
        };

        if (result.Documents.Count == 0)
        {
            _logger?.LogWarning("No documents found in analysis result");
            return dto;
        }

        var document = result.Documents[0];
        var fields = document.Fields;

        // Extract merchant name
        if (fields.TryGetValue("MerchantName", out var merchantField) && merchantField.FieldType == DocumentFieldType.String)
        {
            dto.MerchantName = merchantField.Value.AsString() ?? string.Empty;
            _logger?.LogDebug("Extracted merchant: {Merchant}", dto.MerchantName);
        }

        // Extract transaction date
        if (fields.TryGetValue("TransactionDate", out var dateField) && dateField.FieldType == DocumentFieldType.Date)
        {
            dto.TransactionDate = dateField.Value.AsDate().DateTime;
            _logger?.LogDebug("Extracted date: {Date}", dto.TransactionDate);
        }

        // Extract total amount
        if (fields.TryGetValue("Total", out var totalField))
        {
            dto.Total = ParseCurrencyValue(totalField);
            _logger?.LogDebug("Extracted total: {Total}", dto.Total);
        }

        // Extract tax
        if (fields.TryGetValue("TotalTax", out var taxField))
        {
            dto.Tax = ParseCurrencyValue(taxField);
            _logger?.LogDebug("Extracted tax: {Tax}", dto.Tax);
        }
        else if (fields.TryGetValue("Tax", out var altTaxField))
        {
            dto.Tax = ParseCurrencyValue(altTaxField);
        }

        // Extract subtotal
        if (fields.TryGetValue("Subtotal", out var subtotalField))
        {
            dto.Subtotal = ParseCurrencyValue(subtotalField);
        }
        else
        {
            // Calculate subtotal if not provided
            dto.Subtotal = dto.Total - dto.Tax;
        }

        // Extract currency code (if available)
        if (fields.TryGetValue("Total", out var totalCurrencyField))
        {
            // AsCurrency() returns a struct, so check for exception instead of null
            try
            {
                var currencyValue = totalCurrencyField.Value.AsCurrency();
                var currencyCode = currencyValue.Code; // Use 'Code' property instead of 'CurrencyCode'
                dto.Currency = ParseCurrencyCode(currencyCode);
                _logger?.LogDebug("Extracted currency: {Currency}", dto.Currency);
            }
            catch
            {
                // Ignore if not a currency type
            }
        }

        // Extract line items
        if (fields.TryGetValue("Items", out var itemsField) && itemsField.FieldType == DocumentFieldType.List)
        {
            foreach (var itemField in itemsField.Value.AsList())
            {
                if (itemField.FieldType != DocumentFieldType.Dictionary)
                    continue;

                var itemFields = itemField.Value.AsDictionary();
                var item = ParseLineItem(itemFields);
                if (item != null)
                {
                    dto.Items.Add(item);
                }
            }

            _logger?.LogDebug("Extracted {Count} line items", dto.Items.Count);
        }

        // Calculate confidence score (average of field confidences)
        dto.Confidence = CalculateAverageConfidence(fields);

        return dto;
    }

    /// <summary>
    /// Parse individual line item from Azure response
    /// </summary>
    private ReceiptItemDto? ParseLineItem(IReadOnlyDictionary<string, DocumentField> itemFields)
    {
        var item = new ReceiptItemDto();

        // Description
        if (itemFields.TryGetValue("Description", out var descField) && descField.FieldType == DocumentFieldType.String)
        {
            item.Description = descField.Value.AsString() ?? string.Empty;
        }

        // Quantity
        if (itemFields.TryGetValue("Quantity", out var qtyField))
        {
            if (qtyField.FieldType == DocumentFieldType.Double)
            {
                item.Quantity = (int)Math.Round(qtyField.Value.AsDouble());
            }
            else if (qtyField.FieldType == DocumentFieldType.Int64)
            {
                item.Quantity = (int)qtyField.Value.AsInt64();
            }
        }

        // Total price
        if (itemFields.TryGetValue("TotalPrice", out var priceField))
        {
            item.Price = ParseCurrencyValue(priceField);
        }
        else if (itemFields.TryGetValue("Price", out var altPriceField))
        {
            item.Price = ParseCurrencyValue(altPriceField);
        }

        // Only return item if we have at least a description
        if (string.IsNullOrWhiteSpace(item.Description))
            return null;

        return item;
    }

    /// <summary>
    /// Parse currency value from DocumentField
    /// </summary>
    private decimal ParseCurrencyValue(DocumentField field)
    {
        if (field.FieldType == DocumentFieldType.Currency)
        {
            // AsCurrency() returns a struct, so just use it directly
            var currencyValue = field.Value.AsCurrency();
            return (decimal)currencyValue.Amount;
        }
        else if (field.FieldType == DocumentFieldType.Double)
        {
            return (decimal)field.Value.AsDouble();
        }
        else if (field.FieldType == DocumentFieldType.Int64)
        {
            return field.Value.AsInt64();
        }

        return 0m;
    }

    /// <summary>
    /// Parse ISO currency code to our enum
    /// </summary>
    private CurrencyCode ParseCurrencyCode(string? currencyCode)
    {
        if (string.IsNullOrWhiteSpace(currencyCode))
            return CurrencyCode.USD;

        return currencyCode.ToUpperInvariant() switch
        {
            "USD" => CurrencyCode.USD,
            "EUR" => CurrencyCode.EUR,
            "GBP" => CurrencyCode.GBP,
            "CAD" => CurrencyCode.CAD,
            "AUD" => CurrencyCode.AUD,
            "JPY" => CurrencyCode.JPY,
            "CNY" => CurrencyCode.CNY,
            "INR" => CurrencyCode.INR,
            _ => CurrencyCode.Other
        };
    }

    /// <summary>
    /// Calculate average confidence from all fields
    /// </summary>
    private float CalculateAverageConfidence(IReadOnlyDictionary<string, DocumentField> fields)
    {
        if (fields.Count == 0)
            return 0f;

        var confidences = fields.Values
            .Where(f => f.Confidence.HasValue)
            .Select(f => f.Confidence!.Value)
            .ToList();

        if (confidences.Count == 0)
            return 0.5f; // Default confidence

        return confidences.Average();
    }

    /// <summary>
    /// Create offline result when service is not configured
    /// </summary>
    private ExtractionResultDto CreateOfflineResult()
    {
        return new ExtractionResultDto
        {
            Success = false,
            ErrorMessage = "Azure Document Intelligence is not configured. Please add your API credentials.",
            MerchantName = "Manual Entry Required",
            TransactionDate = DateTime.Now,
            Total = 0,
            Tax = 0,
            Subtotal = 0,
            Currency = CurrencyCode.USD,
            Items = new List<ReceiptItemDto>(),
            Confidence = 0f
        };
    }
}