namespace ReceiptWise.Core.Exceptions;

/// <summary>
/// Exception thrown during AI extraction
/// </summary>
public class ExtractionException : Exception
{
    public ExtractionException(string message) : base(message) { }

    public ExtractionException(string message, Exception innerException)
        : base(message, innerException) { }
}