namespace ReceiptWise.Core.Enums;

/// <summary>
/// Status of AI extraction process
/// </summary>
public enum ExtractionStatus
{
    Pending,
    Processing,
    Completed,
    Failed,
    ManualEntry
}