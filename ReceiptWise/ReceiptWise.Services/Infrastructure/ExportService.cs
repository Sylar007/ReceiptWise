namespace ReceiptWise.Services.Infrastructure;

using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.Extensions.Logging;
using ReceiptWise.Core.Interfaces.Services;
using ReceiptWise.Core.Models.Domain;
using ReceiptWise.Core.Models.DTOs;
using System.Formats.Asn1;
using System.Globalization;
using System.Text;
using Microsoft.Maui.Storage;
/// <summary>
/// Service for exporting receipt data to various formats
/// </summary>
public class ExportService : IExportService
{
    private readonly ILogger<ExportService>? _logger;

    public ExportService(ILogger<ExportService>? logger = null)
    {
        _logger = logger;
    }

    /// <summary>
    /// Export receipts to CSV format
    /// </summary>
    public async Task<string> ExportToCsvAsync(
        IEnumerable<Receipt> receipts,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger?.LogInformation("Exporting {Count} receipts to CSV", receipts.Count());

            var fileName = $"ReceiptWise_Export_{DateTime.Now:yyyyMMdd_HHmmss}.csv";
            var filePath = Path.Combine(FileSystem.CacheDirectory, fileName);

            var config = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                HasHeaderRecord = true,
                Delimiter = ",",
                Quote = '"'
            };

            using var writer = new StreamWriter(filePath, false, Encoding.UTF8);
            using var csv = new CsvWriter(writer, config);

            // Write header
            csv.WriteField("Receipt ID");
            csv.WriteField("Merchant");
            csv.WriteField("Date");
            csv.WriteField("Category");
            csv.WriteField("Total");
            csv.WriteField("Tax");
            csv.WriteField("Subtotal");
            csv.WriteField("Currency");
            csv.WriteField("Item Count");
            csv.WriteField("Notes");
            csv.WriteField("Extraction Status");
            csv.WriteField("Created Date");
            await csv.NextRecordAsync();

            // Write data
            foreach (var receipt in receipts)
            {
                csv.WriteField(receipt.Id);
                csv.WriteField(receipt.MerchantName);
                csv.WriteField(receipt.TransactionDate.ToString("yyyy-MM-dd"));
                csv.WriteField(receipt.Category.ToString());
                csv.WriteField(receipt.Total.ToString("F2"));
                csv.WriteField(receipt.Tax.ToString("F2"));
                csv.WriteField(receipt.Subtotal.ToString("F2"));
                csv.WriteField(receipt.Currency.ToString());
                csv.WriteField(receipt.Items.Count);
                csv.WriteField(receipt.Notes ?? string.Empty);
                csv.WriteField(receipt.ExtractionStatus.ToString());
                csv.WriteField(receipt.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss"));
                await csv.NextRecordAsync();
            }

            await writer.FlushAsync();

            _logger?.LogInformation("CSV export completed: {FilePath}", filePath);
            return filePath;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to export to CSV");
            throw;
        }
    }

    /// <summary>
    /// Export receipts with line items to detailed CSV
    /// </summary>
    public async Task<string> ExportDetailedCsvAsync(
        IEnumerable<Receipt> receipts,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger?.LogInformation("Exporting {Count} receipts with line items to CSV", receipts.Count());

            var fileName = $"ReceiptWise_Detailed_Export_{DateTime.Now:yyyyMMdd_HHmmss}.csv";
            var filePath = Path.Combine(FileSystem.CacheDirectory, fileName);

            using var writer = new StreamWriter(filePath, false, Encoding.UTF8);

            // Write summary section
            await writer.WriteLineAsync("ReceiptWise - Detailed Export");
            await writer.WriteLineAsync($"Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            await writer.WriteLineAsync($"Total Receipts: {receipts.Count()}");
            await writer.WriteLineAsync($"Total Amount: ${receipts.Sum(r => r.Total):F2}");
            await writer.WriteLineAsync();

            // Write receipts with items
            foreach (var receipt in receipts.OrderByDescending(r => r.TransactionDate))
            {
                await writer.WriteLineAsync("--- Receipt ---");
                await writer.WriteLineAsync($"ID,{receipt.Id}");
                await writer.WriteLineAsync($"Merchant,\"{receipt.MerchantName}\"");
                await writer.WriteLineAsync($"Date,{receipt.TransactionDate:yyyy-MM-dd}");
                await writer.WriteLineAsync($"Category,{receipt.Category}");
                await writer.WriteLineAsync($"Total,${receipt.Total:F2}");
                await writer.WriteLineAsync($"Tax,${receipt.Tax:F2}");
                await writer.WriteLineAsync($"Subtotal,${receipt.Subtotal:F2}");

                if (!string.IsNullOrWhiteSpace(receipt.Notes))
                {
                    await writer.WriteLineAsync($"Notes,\"{receipt.Notes}\"");
                }

                // Line items
                if (receipt.Items.Any())
                {
                    await writer.WriteLineAsync();
                    await writer.WriteLineAsync("Item,Quantity,Unit Price,Total Price");
                    foreach (var item in receipt.Items)
                    {
                        await writer.WriteLineAsync(
                            $"\"{item.Description}\",{item.Quantity},{item.UnitPrice:F2},{item.TotalPrice:F2}");
                    }
                }

                await writer.WriteLineAsync();
            }

            _logger?.LogInformation("Detailed CSV export completed: {FilePath}", filePath);
            return filePath;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to export detailed CSV");
            throw;
        }
    }

    /// <summary>
    /// Export receipts to JSON format (for backup)
    /// </summary>
    public async Task<string> ExportToJsonAsync(
        IEnumerable<Receipt> receipts,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger?.LogInformation("Exporting {Count} receipts to JSON", receipts.Count());

            var fileName = $"ReceiptWise_Backup_{DateTime.Now:yyyyMMdd_HHmmss}.json";
            var filePath = Path.Combine(FileSystem.CacheDirectory, fileName);

            var exportData = new
            {
                ExportDate = DateTime.UtcNow,
                Version = "1.0",
                ReceiptCount = receipts.Count(),
                Receipts = receipts.Select(r => new
                {
                    r.Id,
                    r.MerchantName,
                    r.TransactionDate,
                    r.Total,
                    r.Tax,
                    r.Subtotal,
                    Currency = r.Currency.ToString(),
                    Category = r.Category.ToString(),
                    ExtractionStatus = r.ExtractionStatus.ToString(),
                    r.Notes,
                    r.CreatedAt,
                    r.ModifiedAt,
                    Items = r.Items.Select(i => new
                    {
                        i.Description,
                        i.Quantity,
                        i.UnitPrice,
                        i.TotalPrice
                    }),
                    HasWarranty = r.Warranty != null,
                    Warranty = r.Warranty != null ? new
                    {
                        r.Warranty.PurchaseDate,
                        r.Warranty.WarrantyEndDate,
                        r.Warranty.WarrantyMonths,
                        r.Warranty.ProductName,
                        r.Warranty.WarrantyTerms
                    } : null
                })
            };

            var json = System.Text.Json.JsonSerializer.Serialize(exportData, new System.Text.Json.JsonSerializerOptions
            {
                WriteIndented = true
            });

            await File.WriteAllTextAsync(filePath, json, cancellationToken);

            _logger?.LogInformation("JSON export completed: {FilePath}", filePath);
            return filePath;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to export to JSON");
            throw;
        }
    }
}