namespace ReceiptWise.Core.Interfaces.Services;

using ReceiptWise.Core.Models.Domain;

/// <summary>
/// Service for exporting receipt data
/// </summary>
public interface IExportService
{
    Task<string> ExportToCsvAsync(
        IEnumerable<Receipt> receipts,
        CancellationToken cancellationToken = default);
}