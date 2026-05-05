namespace ReceiptWise.Core.Interfaces.Repositories;

using ReceiptWise.Core.Models.Domain;

/// <summary>
/// Repository for attachment operations
/// </summary>
public interface IAttachmentRepository
{
    Task<Attachment?> GetByReceiptIdAsync(int receiptId, CancellationToken cancellationToken = default);

    Task<int> AddAsync(Attachment attachment, CancellationToken cancellationToken = default);

    Task DeleteAsync(int id, CancellationToken cancellationToken = default);
}