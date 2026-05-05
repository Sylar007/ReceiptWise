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
            query = query.Where(r => r.Total >= minAmount.Value);
        }

        if (maxAmount.HasValue)
        {
            query = query.Where(r => r.Total <= maxAmount.Value);
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
                UnitPrice = item.UnitPrice,
                TotalPrice = item.TotalPrice
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
                UnitPrice = item.UnitPrice,
                TotalPrice = item.TotalPrice
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
            Total = entity.Total,
            Tax = entity.Tax,
            Subtotal = entity.Subtotal,
            Currency = (CurrencyCode)entity.Currency,
            Category = (ReceiptCategory)entity.Category,
            ExtractionStatus = (ExtractionStatus)entity.ExtractionStatus,
            Notes = entity.Notes,
            CreatedAt = entity.CreatedAt,
            ModifiedAt = entity.ModifiedAt,
            Items = entity.Items.Select(i => new ReceiptItem
            {
                Id = i.Id,
                ReceiptId = i.ReceiptId,
                Description = i.Description,
                Quantity = i.Quantity,
                UnitPrice = i.UnitPrice,
                TotalPrice = i.TotalPrice
            }).ToList(),
            Attachment = entity.Attachment != null ? new Attachment
            {
                Id = entity.Attachment.Id,
                ReceiptId = entity.Attachment.ReceiptId,
                FileName = entity.Attachment.FileName,
                FilePath = entity.Attachment.FilePath,
                ThumbnailPath = entity.Attachment.ThumbnailPath,
                FileType = entity.Attachment.FileType,
                FileSizeBytes = entity.Attachment.FileSizeBytes,
                CreatedAt = entity.Attachment.CreatedAt
            } : null,
            Warranty = entity.Warranty != null ? new WarrantyInfo
            {
                Id = entity.Warranty.Id,
                ReceiptId = entity.Warranty.ReceiptId,
                PurchaseDate = entity.Warranty.PurchaseDate,
                WarrantyEndDate = entity.Warranty.WarrantyEndDate,
                WarrantyMonths = entity.Warranty.WarrantyMonths,
                ProductName = entity.Warranty.ProductName,
                WarrantyTerms = entity.Warranty.WarrantyTerms,
                NotificationEnabled = entity.Warranty.NotificationEnabled
            } : null
        };
    }

    private ReceiptEntity MapToEntity(Receipt domain)
    {
        return new ReceiptEntity
        {
            Id = domain.Id,
            MerchantName = domain.MerchantName,
            TransactionDate = domain.TransactionDate,
            Total = domain.Total,
            Tax = domain.Tax,
            Subtotal = domain.Subtotal,
            Currency = (int)domain.Currency,
            Category = (int)domain.Category,
            ExtractionStatus = (int)domain.ExtractionStatus,
            Notes = domain.Notes,
            CreatedAt = domain.CreatedAt,
            ModifiedAt = domain.ModifiedAt
        };
    }
}