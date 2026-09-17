using Pharmaceutical.Core.DTOs;

namespace Pharmaceutical.Core.Interfaces;

public interface IDrugService
{
    Task<PagedResult<DrugDto>> GetPagedAsync(string? search, int page, int pageSize, bool? activeOnly = true);
    Task<DrugDto?> GetByIdAsync(string drugId);
    Task<List<DrugDto>> GetLowStockAsync(int threshold);
    Task<bool> AddAsync(DrugCreateDto dto);
    Task<bool> UpdateAsync(string drugId, DrugUpdateDto dto);
    Task<bool> DeactivateAsync(string drugId);
}

public interface IStockService
{
    Task<PagedResult<StockTransactionDto>> GetTransactionsAsync(StockTransactionQueryDto query);
    Task<bool> StockInAsync(StockInDto dto, string operatorName);
    Task<bool> StockOutAsync(StockOutDto dto, string operatorName);
    Task<bool> StockAdjustAsync(StockAdjustDto dto, string operatorName);
    Task<bool> StockReturnAsync(StockOutDto dto, string operatorName);
    Task<bool> StockDamageAsync(StockOutDto dto, string operatorName);
    Task<List<DrugBatchDto>> GetBatchesAsync(string drugId);
    Task<List<ExpiryAlertDto>> GetExpiryAlertsAsync(int withinDays = 90);
}

public interface IPurchaseOrderService
{
    Task<List<PurchaseOrderDto>> GetAllAsync();
    Task<PurchaseOrderDto?> GetByIdAsync(int orderId);
    Task<int> CreateAsync(CreatePurchaseOrderDto dto, string createdBy);
    Task<int> CreateFromLowStockAsync(int supplierId, int threshold, string createdBy);
    Task<bool> SubmitAsync(int orderId);
    Task<bool> ApproveAsync(int orderId);
    Task<bool> ReceiveAsync(int orderId, string operatorName, ReceivePurchaseOrderDto? options = null);
    Task<bool> CancelAsync(int orderId);
}

public interface IAnalyticsService
{
    Task<DashboardDto> GetDashboardAsync(int lowStockThreshold, int expiryDays);
}

public interface IAuditService
{
    Task<List<AuditLogDto>> GetRecentAsync(int count = 100);
}

public interface IExportService
{
    Task<byte[]> ExportDrugsCsvAsync(bool activeOnly = true);
    Task<byte[]> ExportTransactionsCsvAsync(StockTransactionQueryDto query);
    Task<byte[]> ExportLowStockCsvAsync(int threshold);
    Task<byte[]> ExportExpiryAlertsCsvAsync(int withinDays);
}
