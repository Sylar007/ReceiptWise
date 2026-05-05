namespace ReceiptWise.Core.Interfaces.Repositories;

using ReceiptWise.Core.Models.Domain;
using ReceiptWise.Core.Enums;

/// <summary>
/// Repository for receipt CRUD operations
/// </summary>
public interface IReceiptRepository
{
    Task<Receipt?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<IEnumerable<Receipt>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<IEnumerable<Receipt>> SearchAsync(
        string? searchTerm = null,
        ReceiptCategory? category = null,
        decimal? minAmount = null,
        decimal? maxAmount = null,
        DateTime? startDate = null,
        DateTime? endDate = null,
        CancellationToken cancellationToken = default);

    Task<IEnumerable<Receipt>> GetByDateRangeAsync(
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken = default);

    Task<int> AddAsync(Receipt receipt, CancellationToken cancellationToken = default);

    Task UpdateAsync(Receipt receipt, CancellationToken cancellationToken = default);

    Task DeleteAsync(int id, CancellationToken cancellationToken = default);

    Task<int> GetCountAsync(CancellationToken cancellationToken = default);
}