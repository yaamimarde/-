using Pharmaceutical.Core.DTOs;

namespace Pharmaceutical.Core.Interfaces;

public interface IDrugRepository
{
    Task<PagedResult<DrugCatalogEntity>> GetPagedAsync(string? search, int page, int pageSize, bool? activeOnly = true);
    Task<DrugCatalogEntity?> GetByIdAsync(string drugId);
    Task<List<DrugCatalogEntity>> GetLowStockAsync(int threshold);
    Task<bool> AddAsync(DrugCatalogEntity drug);
    Task<bool> UpdateAsync(DrugCatalogEntity drug);
    Task<bool> DeactivateAsync(string drugId);
}

public interface IDrugBatchRepository
{
    Task<List<DrugBatchEntity>> GetByDrugIdAsync(string drugId);
    Task<List<DrugBatchEntity>> GetExpiringAsync(int withinDays);
    Task<DrugBatchEntity?> GetByIdAsync(int batchId);
    Task AddOrUpdateBatchAsync(string drugId, string batchNumber, DateTime expiryDate, DateTime? manufactureDate, int quantity);
    Task<bool> DeductFefoAsync(string drugId, int quantity);
}

public interface IPurchaseOrderRepository
{
    Task<List<PurchaseOrderEntity>> GetAllAsync();
    Task<PurchaseOrderEntity?> GetByIdAsync(int orderId);
    Task<int> CreateAsync(PurchaseOrderEntity order);
    Task<bool> UpdateStatusAsync(int orderId, string status, DateTime? approvedAt = null);
    Task<bool> UpdateLineReceivedAsync(int lineId, int receivedQuantity);
}

public interface IAuditLogRepository
{
    Task AddAsync(AuditLogEntity log);
    Task<List<AuditLogEntity>> GetRecentAsync(int count = 100);
}

public interface IAnalyticsRepository
{
    Task<decimal> GetTotalInventoryValueAsync();
    Task<int> GetActiveDrugCountAsync();
    Task<List<(string DrugId, string DrugName, int TotalOut)>> GetTopMovingDrugsAsync(int days, int top = 10);
}
