namespace ReceiptWise.Data.Entities;

using SQLite;

/// <summary>
/// SQLite entity for Categories table
/// </summary>
[Table("Categories")]
public class CategoryEntity
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    [Unique]
    public int CategoryType { get; set; }

    [MaxLength(100)]
    public string DisplayName { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? Icon { get; set; }

    [MaxLength(20)]
    public string? Color { get; set; }
}