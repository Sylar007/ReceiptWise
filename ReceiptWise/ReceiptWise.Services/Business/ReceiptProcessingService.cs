namespace ReceiptWise.Services.Business;

using Microsoft.Extensions.Logging;
using ReceiptWise.Core.Interfaces.Services;
using ReceiptWise.Core.Interfaces.Repositories;
using ReceiptWise.Core.Models.Domain;
using ReceiptWise.Core.Models.DTOs;
using ReceiptWise.Core.Enums;
using ReceiptWise.Services.Helpers;

/// <summary>
/// Orchestrates the receipt processing workflow:
/// 1. Extract data with Azure Document Intelligence
/// 2. Suggest category (rule-based or AI)
/// 3. Save to database
/// </summary>
public class ReceiptProcessingService
{
    private readonly IReceiptExtractionService _extractionService;
    private readonly IFileStorageService _fileStorageService;
    private readonly IReceiptRepository _receiptRepository;
    private readonly IAttachmentRepository _attachmentRepository;
    private readonly ILogger<ReceiptProcessingService>? _logger;

    public ReceiptProcessingService(
        IReceiptExtractionService extractionService,
        IFileStorageService fileStorageService,
        IReceiptRepository receiptRepository,
        IAttachmentRepository attachmentRepository,
        ILogger<ReceiptProcessingService>? logger = null)
    {
        _extractionService = extractionService;
        _fileStorageService = fileStorageService;
        _receiptRepository = receiptRepository;
        _attachmentRepository = attachmentRepository;
        _logger = logger;
    }

    /// <summary>
    /// Process receipt from file stream: extract, categorize, save
    /// </summary>
    public async Task<ProcessingResult> ProcessReceiptAsync(
        Stream fileStream,
        string fileName,
        CancellationToken cancellationToken = default)
    {
        var result = new ProcessingResult();

        try
        {
            _logger?.LogInformation("Starting receipt processing for file: {FileName}", fileName);

            // Step 1: Save file to storage
            result.Status = ProcessingStatus.SavingFile;
            var filePath = await _fileStorageService.SaveFileAsync(fileStream, fileName, cancellationToken);
            result.FilePath = filePath;

            var mimeType = ImageHelper.GetMimeType(fileName);

            // Step 2: Extract data with AI
            result.Status = ProcessingStatus.Extracting;
            fileStream.Position = 0;

            var extractionResult = await _extractionService.ExtractReceiptDataAsync(
                fileStream,
                mimeType,
                cancellationToken);

            result.ExtractionResult = extractionResult;

            if (!extractionResult.Success)
            {
                _logger?.LogWarning("Extraction failed: {Error}", extractionResult.ErrorMessage);
                result.Status = ProcessingStatus.ExtractionFailed;
                result.ErrorMessage = extractionResult.ErrorMessage;

                // Save as manual entry even if extraction fails
                await SaveManualEntryAsync(filePath, fileName, mimeType, cancellationToken);
                result.Status = ProcessingStatus.SavedAsManual;
                return result;
            }

            // Step 3: Create receipt from extracted data
            result.Status = ProcessingStatus.Saving;
            var receipt = MapToReceipt(extractionResult);
            receipt.ExtractionStatus = ExtractionStatus.Completed;

            var receiptId = await _receiptRepository.AddAsync(receipt, cancellationToken);
            result.ReceiptId = receiptId;

            // Step 4: Link attachment
            var attachment = new Attachment
            {
                ReceiptId = receiptId,
                FileName = fileName,
                FilePath = filePath,
                FileType = mimeType,
                FileSizeBytes = new FileInfo(filePath).Length
            };

            await _attachmentRepository.AddAsync(attachment, cancellationToken);

            result.Status = ProcessingStatus.Completed;
            result.Success = true;

            _logger?.LogInformation(
                "Receipt processing completed successfully. ID={ReceiptId}, Merchant={Merchant}",
                receiptId,
                receipt.MerchantName);

            return result;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Receipt processing failed for file: {FileName}", fileName);
            result.Status = ProcessingStatus.Failed;
            result.Success = false;
            result.ErrorMessage = ex.Message;
            return result;
        }
    }

    /// <summary>
    /// Save as manual entry when extraction fails
    /// </summary>
    private async Task<int> SaveManualEntryAsync(
        string filePath,
        string fileName,
        string mimeType,
        CancellationToken cancellationToken)
    {
        var receipt = new Receipt
        {
            MerchantName = "Manual Entry Required",
            TransactionDate = DateTime.Now,
            Total = 0,
            Tax = 0,
            Subtotal = 0,
            Currency = CurrencyCode.USD,
            Category = ReceiptCategory.Other,
            ExtractionStatus = ExtractionStatus.Failed,
            Notes = "Automatic extraction failed. Please edit manually."
        };

        var receiptId = await _receiptRepository.AddAsync(receipt, cancellationToken);

        var attachment = new Attachment
        {
            ReceiptId = receiptId,
            FileName = fileName,
            FilePath = filePath,
            FileType = mimeType,
            FileSizeBytes = new FileInfo(filePath).Length
        };

        await _attachmentRepository.AddAsync(attachment, cancellationToken);

        return receiptId;
    }

    /// <summary>
    /// Map extraction result to receipt domain model
    /// </summary>
    private Receipt MapToReceipt(ExtractionResultDto dto)
    {
        var receipt = new Receipt
        {
            MerchantName = dto.MerchantName,
            TransactionDate = dto.TransactionDate ?? DateTime.Now,
            Total = dto.Total,
            Tax = dto.Tax,
            Subtotal = dto.Subtotal,
            Currency = dto.Currency,
            Category = ReceiptCategory.Other, // Will be set by categorization in Milestone 5
            ExtractionStatus = ExtractionStatus.Completed,
            Notes = dto.Confidence < 0.7f ? $"Low confidence extraction ({dto.Confidence:P0})" : null
        };

        // Map line items
        receipt.Items = dto.Items.Select(itemDto => new ReceiptItem
        {
            Description = itemDto.Description,
            Quantity = itemDto.Quantity,
            UnitPrice = itemDto.Price / (itemDto.Quantity > 0 ? itemDto.Quantity : 1),
            TotalPrice = itemDto.Price
        }).ToList();

        return receipt;
    }
}

/// <summary>
/// Result of receipt processing operation
/// </summary>
public class ProcessingResult
{
    public bool Success { get; set; }
    public ProcessingStatus Status { get; set; }
    public string? ErrorMessage { get; set; }
    public int ReceiptId { get; set; }
    public string? FilePath { get; set; }
    public ExtractionResultDto? ExtractionResult { get; set; }
}

public enum ProcessingStatus
{
    NotStarted,
    SavingFile,
    Extracting,
    Saving,
    Completed,
    ExtractionFailed,
    SavedAsManual,
    Failed
}