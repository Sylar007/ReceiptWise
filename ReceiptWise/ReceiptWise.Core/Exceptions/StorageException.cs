namespace ReceiptWise.Core.Exceptions;

/// <summary>
/// Exception thrown during file storage operations
/// </summary>
public class StorageException : Exception
{
    public StorageException(string message) : base(message) { }

    public StorageException(string message, Exception innerException)
        : base(message, innerException) { }
}