# ReceiptWise Database Documentation

## Overview
ReceiptWise uses SQLite with **sqlite-net-pcl** for local-first data storage.

## Schema

### Tables

#### Receipts
Primary table for receipt records.

| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| Id | INTEGER | PRIMARY KEY, AUTOINCREMENT | Unique identifier |
| MerchantName | TEXT(500) | NOT NULL | Store/merchant name |
| TransactionDate | DATETIME | NOT NULL | Purchase date |
| Total | DECIMAL | NOT NULL | Total amount |
| Tax | DECIMAL | NOT NULL | Tax amount |
| Subtotal | DECIMAL | NOT NULL | Pre-tax amount |
| Currency | INTEGER | NOT NULL | Currency code (enum) |
| Category | INTEGER | NOT NULL | Category (enum) |
| ExtractionStatus | INTEGER | NOT NULL | AI extraction status |
| Notes | TEXT(1000) | NULL | User notes |
| CreatedAt | DATETIME | NOT NULL | Record creation timestamp |
| ModifiedAt | DATETIME | NULL | Last modification timestamp |

**Indexes:**
- `idx_receipts_date` on `TransactionDate DESC`
- `idx_receipts_merchant` on `MerchantName`
- `idx_receipts_category` on `Category`

---

#### ReceiptItems
Line items for each receipt.

| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| Id | INTEGER | PRIMARY KEY, AUTOINCREMENT | Unique identifier |
| ReceiptId | INTEGER | NOT NULL, INDEXED | Foreign key to Receipts |
| Description | TEXT(500) | NOT NULL | Item description |
| Quantity | INTEGER | NOT NULL | Item quantity |
| UnitPrice | DECIMAL | NOT NULL | Price per unit |
| TotalPrice | DECIMAL | NOT NULL | Total item price |

**Indexes:**
- `idx_items_receipt` on `ReceiptId`

---

#### Attachments
Images/PDFs attached to receipts.

| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| Id | INTEGER | PRIMARY KEY, AUTOINCREMENT | Unique identifier |
| ReceiptId | INTEGER | UNIQUE, INDEXED | Foreign key to Receipts |
| FileName | TEXT(255) | NOT NULL | Original filename |
| FilePath | TEXT(1000) | NOT NULL | Local file path |
| ThumbnailPath | TEXT(1000) | NULL | Thumbnail path |
| FileType | TEXT(50) | NOT NULL | MIME type |
| FileSizeBytes | INTEGER | NOT NULL | File size in bytes |
| CreatedAt | DATETIME | NOT NULL | Upload timestamp |

**Indexes:**
- `idx_attachments_receipt` on `ReceiptId`

---

#### Categories
Predefined receipt categories.

| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| Id | INTEGER | PRIMARY KEY, AUTOINCREMENT | Unique identifier |
| CategoryType | INTEGER | UNIQUE | Category enum value |
| DisplayName | TEXT(100) | NOT NULL | Human-readable name |
| Icon | TEXT(100) | NULL | Emoji/icon |
| Color | TEXT(20) | NULL | Hex color code |

---

#### WarrantyInfo
Product warranty information.

| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| Id | INTEGER | PRIMARY KEY, AUTOINCREMENT | Unique identifier |
| ReceiptId | INTEGER | UNIQUE, INDEXED | Foreign key to Receipts |
| PurchaseDate | DATETIME | NOT NULL | Purchase date |
| WarrantyEndDate | DATETIME | NOT NULL | Warranty expiration |
| WarrantyMonths | INTEGER | NOT NULL | Warranty duration |
| ProductName | TEXT(500) | NULL | Product name |
| WarrantyTerms | TEXT(1000) | NULL | Warranty details |
| NotificationEnabled | BOOLEAN | NOT NULL | Enable reminders |

---

## Migrations

Current version: **1**

### Version 1 (Initial)
- Created all base tables
- Added indexes for performance
- Seeded default categories

---

## Sample Data

In DEBUG builds, 15 sample receipts are automatically seeded on first launch.

### Seeding Logic:
- Random merchants from 12 predefined stores
- Transaction dates within last 90 days
- 1-5 line items per receipt
- Realistic pricing and tax calculations
- 30% of technology purchases include warranties

---

## Performance Considerations

### Indexes
All frequently queried fields are indexed:
- Transaction date (for date range queries)
- Merchant name (for search)
- Category (for filtering)

### Query Optimization
- Use parameterized queries to prevent SQL injection
- Load related data (items, attachments) only when needed
- Batch operations for bulk inserts

---

## Backup & Recovery

### Location
Database file: `{AppDataDirectory}/receiptwise.db3`

**Platforms:**
- **Android**: `/data/data/com.receiptwise.app/files/`
- **iOS**: `~/Library/Application Support/`
- **Windows**: `%LOCALAPPDATA%\Packages\...\LocalState\`

### Backup Strategy
1. Export to CSV (Milestone 8)
2. Cloud sync (Future: Milestone 11+)

---

## Development Tools

### Clear All Data
Settings → Developer Tools → Clear All Data

### Seed Sample Data
Settings → Developer Tools → Seed Sample Data (20 receipts)

### View Database
Use **DB Browser for SQLite** to inspect the database file manually.

---

## Testing

### Unit Tests
- **ReceiptRepositoryTests**: CRUD operations
- **CategoryRepositoryTests**: Category initialization
- **AttachmentRepositoryTests**: File attachment handling
- **DatabaseTests**: Schema validation, seeding

### Test Database
Each test creates an isolated in-memory database to avoid side effects.

---

## Future Enhancements

1. **Database Encryption** (Milestone 10)
   - Encrypt sensitive data using SQLCipher
2. **Cloud Sync** (Post-MVP)
   - Sync receipts to Azure SQL/Cosmos DB
3. **Automatic Backups** (Post-MVP)
   - Daily local backups with rotation

---

Last Updated: Milestone 2