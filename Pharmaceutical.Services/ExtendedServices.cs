using Microsoft.Extensions.Options;
using Pharmaceutical.Core;
using Pharmaceutical.Core.DTOs;
using Pharmaceutical.Core.Interfaces;
using Pharmaceutical.Core.Settings;

namespace Pharmaceutical.Services;

public class PurchaseOrderService : IPurchaseOrderService
{
    private readonly IPurchaseOrderRepository _repository;
    private readonly IDrugRepository _drugRepository;
    private readonly IStockService _stockService;

    public PurchaseOrderService(
        IPurchaseOrderRepository repository,
        IDrugRepository drugRepository,
        IStockService stockService)
    {
        _repository = repository;
        _drugRepository = drugRepository;
        _stockService = stockService;
    }

    public async Task<List<PurchaseOrderDto>> GetAllAsync()
    {
        var orders = await _repository.GetAllAsync();
        return orders.Select(MapToDto).ToList();
    }

    public async Task<PurchaseOrderDto?> GetByIdAsync(int orderId)
    {
        var order = await _repository.GetByIdAsync(orderId);
        return order == null ? null : MapToDto(order);
    }

    public async Task<int> CreateAsync(CreatePurchaseOrderDto dto, string createdBy)
    {
        var order = new PurchaseOrderEntity
        {
            SupplierId = dto.SupplierId,
            Status = PurchaseOrderStatus.Draft,
            CreatedBy = createdBy,
            CreatedAt = DateTime.UtcNow,
            Lines = dto.Lines.Select(l => new PurchaseOrderLineEntity
            {
                DrugId = l.DrugId,
                Quantity = l.Quantity
            }).ToList()
        };
        return await _repository.CreateAsync(order);
    }

    public async Task<int> CreateFromLowStockAsync(int supplierId, int threshold, string createdBy)
    {
        var lowStock = await _drugRepository.GetLowStockAsync(threshold);
        var lines = lowStock
            .Where(d => d.SupplierId == supplierId)
            .Select(d => new PurchaseOrderLineCreateDto
            {
                DrugId = d.DrugId,
                Quantity = Math.Max(threshold - d.StockQuantity, 10)
            }).ToList();

        if (lines.Count == 0) return 0;

        return await CreateAsync(new CreatePurchaseOrderDto { SupplierId = supplierId, Lines = lines }, createdBy);
    }

    public async Task<bool> SubmitAsync(int orderId) =>
        await _repository.UpdateStatusAsync(orderId, PurchaseOrderStatus.Pending);

    public async Task<bool> ApproveAsync(int orderId) =>
        await _repository.UpdateStatusAsync(orderId, PurchaseOrderStatus.Approved, DateTime.UtcNow);

    public async Task<bool> ReceiveAsync(int orderId, string operatorName, ReceivePurchaseOrderDto? options = null)
    {
        var order = await _repository.GetByIdAsync(orderId);
        if (order == null || order.Status != PurchaseOrderStatus.Approved) return false;

        foreach (var line in order.Lines)
        {
            var remaining = line.Quantity - line.ReceivedQuantity;
            if (remaining <= 0) continue;

            var batchNumber = options?.BatchNumber ?? $"PO-{orderId}-{line.DrugId}";
            var expiry = options?.ExpiryDate ?? DateTime.UtcNow.Date.AddYears(2);

            var success = await _stockService.StockInAsync(new StockInDto
            {
                DrugId = line.DrugId,
                Quantity = remaining,
                BatchNumber = batchNumber,
                ExpiryDate = expiry,
                Remark = $"采购单 #{orderId} 收货"
            }, operatorName);

            if (!success) return false;
            await _repository.UpdateLineReceivedAsync(line.LineId, remaining);
        }

        return await _repository.UpdateStatusAsync(orderId, PurchaseOrderStatus.Received);
    }

    public async Task<bool> CancelAsync(int orderId)
    {
        var order = await _repository.GetByIdAsync(orderId);
        if (order == null) return false;
        if (order.Status is not (PurchaseOrderStatus.Draft or PurchaseOrderStatus.Pending))
            return false;
        return await _repository.UpdateStatusAsync(orderId, PurchaseOrderStatus.Cancelled);
    }

    private static PurchaseOrderDto MapToDto(PurchaseOrderEntity o) => new()
    {
        OrderId = o.OrderId,
        SupplierId = o.SupplierId,
        SupplierName = o.Supplier?.Name ?? string.Empty,
        Status = o.Status,
        CreatedBy = o.CreatedBy,
        CreatedAt = o.CreatedAt,
        ApprovedAt = o.ApprovedAt,
        Lines = o.Lines.Select(l => new PurchaseOrderLineDto
        {
            LineId = l.LineId,
            DrugId = l.DrugId,
            DrugName = l.Drug?.DrugName ?? string.Empty,
            Quantity = l.Quantity,
            ReceivedQuantity = l.ReceivedQuantity
        }).ToList()
    };
}

public class AnalyticsService : IAnalyticsService
{
    private readonly IAnalyticsRepository _analyticsRepository;
    private readonly IDrugRepository _drugRepository;
    private readonly IStockService _stockService;
    private readonly IPurchaseOrderRepository _purchaseOrderRepository;
    private readonly AlertSettings _alertSettings;

    public AnalyticsService(
        IAnalyticsRepository analyticsRepository,
        IDrugRepository drugRepository,
        IStockService stockService,
        IPurchaseOrderRepository purchaseOrderRepository,
        IOptions<AlertSettings> alertSettings)
    {
        _analyticsRepository = analyticsRepository;
        _drugRepository = drugRepository;
        _stockService = stockService;
        _purchaseOrderRepository = purchaseOrderRepository;
        _alertSettings = alertSettings.Value;
    }

    public async Task<DashboardDto> GetDashboardAsync(int lowStockThreshold, int expiryDays)
    {
        var lowStock = await _drugRepository.GetLowStockAsync(lowStockThreshold);
        var expiryAlerts = await _stockService.GetExpiryAlertsAsync(expiryDays);
        var topMoving = await _analyticsRepository.GetTopMovingDrugsAsync(30, 10);
        var orders = await _purchaseOrderRepository.GetAllAsync();

        var topDict = topMoving.ToDictionary(t => t.DrugId, t => t.TotalOut);
        var suggestions = new List<ReorderSuggestionDto>();
        foreach (var drug in lowStock.Take(20))
        {
            var totalOut = topDict.GetValueOrDefault(drug.DrugId);
            var dailyAvg = totalOut > 0 ? totalOut / 30.0 : 1;
            var daysUntil = (int)(drug.StockQuantity / dailyAvg);
            suggestions.Add(new ReorderSuggestionDto
            {
                DrugId = drug.DrugId,
                DrugName = drug.DrugName,
                CurrentStock = drug.StockQuantity,
                DailyAverageOut = Math.Round(dailyAvg, 2),
                SuggestedReorderQuantity = Math.Max((int)(dailyAvg * 14), lowStockThreshold - drug.StockQuantity),
                DaysUntilStockout = daysUntil
            });
        }

        return new DashboardDto
        {
            TotalInventoryValue = await _analyticsRepository.GetTotalInventoryValueAsync(),
            LowStockCount = lowStock.Count,
            ExpiryAlertCount = expiryAlerts.Count,
            ActiveDrugCount = await _analyticsRepository.GetActiveDrugCountAsync(),
            PendingPurchaseOrders = orders.Count(o => o.Status == PurchaseOrderStatus.Pending),
            TopMovingDrugs = topMoving.Select(t => new DrugMovementDto
            {
                DrugId = t.DrugId,
                DrugName = t.DrugName,
                TotalOutQuantity = t.TotalOut
            }).ToList(),
            ReorderSuggestions = suggestions
        };
    }
}

public class AuditService : IAuditService
{
    private readonly IAuditLogRepository _repository;

    public AuditService(IAuditLogRepository repository)
    {
        _repository = repository;
    }

    public async Task<List<AuditLogDto>> GetRecentAsync(int count = 100)
    {
        var logs = await _repository.GetRecentAsync(count);
        return logs.Select(l => new AuditLogDto
        {
            AuditId = l.AuditId,
            EntityType = l.EntityType,
            EntityId = l.EntityId,
            Action = l.Action,
            UserName = l.UserName,
            OldValues = l.OldValues,
            NewValues = l.NewValues,
            CreatedAt = l.CreatedAt
        }).ToList();
    }
}

public class ExportService : IExportService
{
    private readonly IDrugRepository _drugRepository;
    private readonly IStockRepository _stockRepository;
    private readonly IStockService _stockService;

    public ExportService(
        IDrugRepository drugRepository,
        IStockRepository stockRepository,
        IStockService stockService)
    {
        _drugRepository = drugRepository;
        _stockRepository = stockRepository;
        _stockService = stockService;
    }

    public async Task<byte[]> ExportDrugsCsvAsync(bool activeOnly = true)
    {
        var result = await _drugRepository.GetPagedAsync(null, 1, 10000, activeOnly);
        var lines = new List<string> { "药品编号,名称,库存,采购价,零售价,供应商ID,状态" };
        lines.AddRange(result.Items.Select(d =>
            $"{d.DrugId},{d.DrugName},{d.StockQuantity},{d.PurchasePrice},{d.RetailPrice},{d.SupplierId},{(d.IsActive ? "在售" : "下架")}"));
        return System.Text.Encoding.UTF8.GetBytes(string.Join('\n', lines));
    }

    public async Task<byte[]> ExportTransactionsCsvAsync(StockTransactionQueryDto query)
    {
        var (items, _) = await _stockRepository.GetPagedAsync(
            query.DrugId, query.TransactionType, query.FromDate, query.ToDate, 1, 10000);
        var lines = new List<string> { "流水ID,药品编号,类型,数量,操作人,时间,备注" };
        lines.AddRange(items.Select(t =>
            $"{t.TransactionId},{t.DrugId},{t.TransactionType},{t.Quantity},{t.Operator},{t.CreatedAt:yyyy-MM-dd HH:mm},{t.Remark}"));
        return System.Text.Encoding.UTF8.GetBytes(string.Join('\n', lines));
    }

    public async Task<byte[]> ExportLowStockCsvAsync(int threshold)
    {
        var drugs = await _drugRepository.GetLowStockAsync(threshold);
        var lines = new List<string> { "药品编号,名称,库存,供应商" };
        lines.AddRange(drugs.Select(d => $"{d.DrugId},{d.DrugName},{d.StockQuantity},{d.Supplier?.Name}"));
        return System.Text.Encoding.UTF8.GetBytes(string.Join('\n', lines));
    }

    public async Task<byte[]> ExportExpiryAlertsCsvAsync(int withinDays)
    {
        var alerts = await _stockService.GetExpiryAlertsAsync(withinDays);
        var lines = new List<string> { "药品编号,药品名称,批号,效期,剩余天数,数量,级别" };
        lines.AddRange(alerts.Select(a =>
            $"{a.DrugId},{a.DrugName},{a.BatchNumber},{a.ExpiryDate:yyyy-MM-dd},{a.DaysUntilExpiry},{a.Quantity},{a.AlertLevel}"));
        return System.Text.Encoding.UTF8.GetBytes(string.Join('\n', lines));
    }
}
