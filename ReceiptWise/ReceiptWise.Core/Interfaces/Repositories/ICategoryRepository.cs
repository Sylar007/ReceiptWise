namespace ReceiptWise.Core.Interfaces.Repositories;

using ReceiptWise.Core.Models.Domain;

/// <summary>
/// Repository for category operations
/// </summary>
public interface ICategoryRepository
{
    Task<IEnumerable<Category>> GetAllAsync(CancellationToken cancellationToken = default);

    Task InitializeDefaultCategoriesAsync(CancellationToken cancellationToken = default);
}