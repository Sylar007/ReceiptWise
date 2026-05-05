namespace ReceiptWise.Data.Repositories;

using ReceiptWise.Core.Interfaces.Repositories;
using ReceiptWise.Core.Models.Domain;
using ReceiptWise.Data.Context;
using ReceiptWise.Data.Entities;

/// <summary>
/// SQLite implementation of IAttachmentRepository
/// </summary>
public class AttachmentRepository : IAttachmentRepository
{
    private readonly ReceiptWiseDatabase _database;

    public AttachmentRepository(ReceiptWiseDatabase database)
    {
        _database = database;
    }

    public async Task<Attachment?> GetByReceiptIdAsync(int receiptId, CancellationToken cancellationToken = default)
    {
        var conn = _database.GetConnection();
        var entity = await conn.Table<AttachmentEntity>()
            .Where(a => a.ReceiptId == receiptId)
            .FirstOrDefaultAsync();

        if (entity == null)
            return null;

        return new Attachment
        {
            Id = entity.Id,
            ReceiptId = entity.ReceiptId,
            FileName = entity.FileName,
            FilePath = entity.FilePath,
            ThumbnailPath = entity.ThumbnailPath,
            FileType = entity.FileType,
            FileSizeBytes = entity.FileSizeBytes,
            CreatedAt = entity.CreatedAt
        };
    }

    public async Task<int> AddAsync(Attachment attachment, CancellationToken cancellationToken = default)
    {
        var conn = _database.GetConnection();
        var entity = new AttachmentEntity
        {
            ReceiptId = attachment.ReceiptId,
            FileName = attachment.FileName,
            FilePath = attachment.FilePath,
            ThumbnailPath = attachment.ThumbnailPath,
            FileType = attachment.FileType,
            FileSizeBytes = attachment.FileSizeBytes,
            CreatedAt = DateTime.UtcNow
        };

        await conn.InsertAsync(entity);
        return entity.Id;
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var conn = _database.GetConnection();
        await conn.DeleteAsync<AttachmentEntity>(id);
    }
}