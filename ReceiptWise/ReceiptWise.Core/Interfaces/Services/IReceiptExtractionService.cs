namespace ReceiptWise.Core.Interfaces.Services;

using ReceiptWise.Core.Models.DTOs;

/// <summary>
/// Service for extracting receipt data using Azure Document Intelligence
/// </summary>
public interface IReceiptExtractionService
{
    /// <summary>
    /// Extract receipt data from image or PDF
    /// </summary>
    Task<ExtractionResultDto> ExtractReceiptDataAsync(
        Stream fileStream,
        string contentType,
        CancellationToken cancellationToken = default);
}