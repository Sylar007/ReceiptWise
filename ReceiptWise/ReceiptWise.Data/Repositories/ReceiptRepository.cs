namespace ReceiptWise.Data.Repositories;

using ReceiptWise.Core.Interfaces.Repositories;
using ReceiptWise.Core.Models.Domain;
using ReceiptWise.Core.Enums;
using ReceiptWise.Data.Context;
using ReceiptWise.Data.Entities;

/// <summary>
/// SQLite implementation of IReceiptRepository
/// </summary>
public class ReceiptRepository : IReceiptRepository
{
    private readonly ReceiptWiseDatabase _database;

    public ReceiptRepository(ReceiptWiseDatabase database)
    {
        _database = database;
    }

    public async Task<Receipt?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var conn = _database.GetConnection();
        var entity = await conn.FindAsync<ReceiptEntity>(id);

        if (entity == null)
            return null;

        // Load related data
        entity.Items = await conn.Table<ReceiptItemEntity>()
            .Where(i => i.ReceiptId == id)
            .ToListAsync();

        entity.Attachment = await conn.Table<AttachmentEntity>()
            .Where(a => a.ReceiptId == id)
            .FirstOrDefaultAsync();

        entity.Warranty = await conn.Table<WarrantyInfoEntity>()
            .Where(w => w.ReceiptId == id)
            .FirstOrDefaultAsync();

        return MapToDomain(entity);
    }

    public async Task<IEnumerable<Receipt>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var conn = _database.GetConnection();
        var entities = await conn.Table<ReceiptEntity>()
            .OrderByDescending(r => r.TransactionDate)
            .ToListAsync();

        var receipts = new List<Receipt>();
        foreach (var entity in entities)
        {
            entity.Items = await conn.Table<ReceiptItemEntity>()
                .Where(i => i.ReceiptId == entity.Id)
                .ToListAsync();

            entity.Attachment = await conn.Table<AttachmentEntity>()
                .Where(a => a.ReceiptId == entity.Id)
                .FirstOrDefaultAsync();

            receipts.Add(MapToDomain(entity));
        }

        return receipts;
    }

    public async Task<IEnumerable<Receipt>> SearchAsync(
        string? searchTerm = null,
        ReceiptCategory? category = null,
        decimal? minAmount = null,
        decimal? maxAmount = null,
        DateTime? startDate = null,
        DateTime? endDate = null,
        CancellationToken cancellationToken = default)
    {
        var conn = _database.GetConnection();
        var query = conn.Table<ReceiptEntity>();

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            query = query.Where(r => r.MerchantName.Contains(searchTerm));
        }

        if (category.HasValue)
        {
            query = query.Where(r => r.Category == (int)category.Value);
        }

        if (minAmount.HasValue)
        {
            query = query.Where(r => r.Total >= (double)minAmount.Value);
        }

        if (maxAmount.HasValue)
        {
            query = query.Where(r => r.Total <= (double)maxAmount.Value);
        }

        if (startDate.HasValue)
        {
            query = query.Where(r => r.TransactionDate >= startDate.Value);
        }

        if (endDate.HasValue)
        {
            query = query.Where(r => r.TransactionDate <= endDate.Value);
        }

        var entities = await query.OrderByDescending(r => r.TransactionDate).ToListAsync();

        var receipts = new List<Receipt>();
        foreach (var entity in entities)
        {
            entity.Items = await conn.Table<ReceiptItemEntity>()
                .Where(i => i.ReceiptId == entity.Id)
                .ToListAsync();

            entity.Attachment = await conn.Table<AttachmentEntity>()
                .Where(a => a.ReceiptId == entity.Id)
                .FirstOrDefaultAsync();

            receipts.Add(MapToDomain(entity));
        }

        return receipts;
    }

    public async Task<IEnumerable<Receipt>> GetByDateRangeAsync(
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken = default)
    {
        return await SearchAsync(
            startDate: startDate,
            endDate: endDate,
            cancellationToken: cancellationToken);
    }

    public async Task<int> AddAsync(Receipt receipt, CancellationToken cancellationToken = default)
    {
        var conn = _database.GetConnection();
        var entity = MapToEntity(receipt);
        entity.CreatedAt = DateTime.UtcNow;

        await conn.InsertAsync(entity);

        // Insert items
        foreach (var item in receipt.Items)
        {
            var itemEntity = new ReceiptItemEntity
            {
                ReceiptId = entity.Id,
                Description = item.Description,
                Quantity = item.Quantity,
                UnitPrice = (double)item.UnitPrice,
                TotalPrice = (double)item.TotalPrice
            };
            await conn.InsertAsync(itemEntity);
        }

        // Insert attachment if exists
        if (receipt.Attachment != null)
        {
            var attachmentEntity = new AttachmentEntity
            {
                ReceiptId = entity.Id,
                FileName = receipt.Attachment.FileName,
                FilePath = receipt.Attachment.FilePath,
                ThumbnailPath = receipt.Attachment.ThumbnailPath,
                FileType = receipt.Attachment.FileType,
                FileSizeBytes = receipt.Attachment.FileSizeBytes,
                CreatedAt = DateTime.UtcNow
            };
            await conn.InsertAsync(attachmentEntity);
        }

        return entity.Id;
    }

    public async Task UpdateAsync(Receipt receipt, CancellationToken cancellationToken = default)
    {
        var conn = _database.GetConnection();
        var entity = MapToEntity(receipt);
        entity.ModifiedAt = DateTime.UtcNow;

        await conn.UpdateAsync(entity);

        // Update items (simple approach: delete all and re-insert)
        await conn.ExecuteAsync("DELETE FROM ReceiptItems WHERE ReceiptId = ?", receipt.Id);

        foreach (var item in receipt.Items)
        {
            var itemEntity = new ReceiptItemEntity
            {
                ReceiptId = receipt.Id,
                Description = item.Description,
                Quantity = item.Quantity,
                UnitPrice = (double)item.UnitPrice,
                TotalPrice = (double)item.TotalPrice
            };
            await conn.InsertAsync(itemEntity);
        }
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var conn = _database.GetConnection();

        // Delete related records (cascade)
        await conn.ExecuteAsync("DELETE FROM ReceiptItems WHERE ReceiptId = ?", id);
        await conn.ExecuteAsync("DELETE FROM Attachments WHERE ReceiptId = ?", id);
        await conn.ExecuteAsync("DELETE FROM WarrantyInfo WHERE ReceiptId = ?", id);

        await conn.DeleteAsync<ReceiptEntity>(id);
    }

    public async Task<int> GetCountAsync(CancellationToken cancellationToken = default)
    {
        var conn = _database.GetConnection();
        return await conn.Table<ReceiptEntity>().CountAsync();
    }

    // Mapping helpers
    private Receipt MapToDomain(ReceiptEntity entity)
    {
        return new Receipt
        {
            Id = entity.Id,
            MerchantName = entity.MerchantName,
            TransactionDate = entity.TransactionDate,
            Total = (decimal)entity.Total,  // Convert double to decimal
            Tax = (decimal)entity.Tax,
            Subtotal = (decimal)entity.Subtotal,
            Currency = (CurrencyCode)entity.Currency,
            Category = (ReceiptCategory)entity.Category,
            ExtractionStatus = (ExtractionStatus)entity.ExtractionStatus,
            Notes = entity.Notes,
            CreatedAt = entity.CreatedAt,
            ModifiedAt = entity.ModifiedAt,
            Items = entity.Items.Select(MapItemToDomain).ToList()
        };
    }

    private ReceiptEntity MapToEntity(Receipt receipt)
    {
        var entity = new ReceiptEntity
        {
            Id = receipt.Id,
            MerchantName = receipt.MerchantName,
            TransactionDate = receipt.TransactionDate,
            Total = (double)receipt.Total,  // Convert decimal to double
            Tax = (double)receipt.Tax,
            Subtotal = (double)receipt.Subtotal,
            Currency = (int)receipt.Currency,
            Category = (int)receipt.Category,
            ExtractionStatus = (int)receipt.ExtractionStatus,
            Notes = receipt.Notes,
            CreatedAt = receipt.CreatedAt,
            ModifiedAt = receipt.ModifiedAt
        };

        return entity;
    }

    private ReceiptItem MapItemToDomain(ReceiptItemEntity entity)
    {
        return new ReceiptItem
        {
            Id = entity.Id,
            ReceiptId = entity.ReceiptId,
            Description = entity.Description,
            Quantity = entity.Quantity,
            UnitPrice = (decimal)entity.UnitPrice,  // Convert double to decimal
            TotalPrice = (decimal)entity.TotalPrice
        };
    }

    private ReceiptItemEntity MapItemToEntity(ReceiptItem item)
    {
        return new ReceiptItemEntity
        {
            Id = item.Id,
            ReceiptId = item.ReceiptId,
            Description = item.Description,
            Quantity = item.Quantity,
            UnitPrice = (double)item.UnitPrice,  // Convert decimal to double
            TotalPrice = (double)item.TotalPrice
        };
    }
}