using Microsoft.EntityFrameworkCore;
using Pharmaceutical.Core;
using Pharmaceutical.Core.Interfaces;

namespace Pharmaceutical.Infrastructure.Repositories;

public class PurchaseOrderRepository : IPurchaseOrderRepository
{
    private readonly PharmaceuticalDbContext _context;

    public PurchaseOrderRepository(PharmaceuticalDbContext context)
    {
        _context = context;
    }

    public async Task<List<PurchaseOrderEntity>> GetAllAsync() =>
        await _context.PurchaseOrders
            .Include(o => o.Supplier)
            .Include(o => o.Lines).ThenInclude(l => l.Drug)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync();

    public async Task<PurchaseOrderEntity?> GetByIdAsync(int orderId) =>
        await _context.PurchaseOrders
            .Include(o => o.Supplier)
            .Include(o => o.Lines).ThenInclude(l => l.Drug)
            .FirstOrDefaultAsync(o => o.OrderId == orderId);

    public async Task<int> CreateAsync(PurchaseOrderEntity order)
    {
        await _context.PurchaseOrders.AddAsync(order);
        await _context.SaveChangesAsync();
        return order.OrderId;
    }

    public async Task<bool> UpdateStatusAsync(int orderId, string status, DateTime? approvedAt = null)
    {
        var order = await _context.PurchaseOrders.FindAsync(orderId);
        if (order == null) return false;
        order.Status = status;
        if (approvedAt.HasValue) order.ApprovedAt = approvedAt;
        return await _context.SaveChangesAsync() > 0;
    }

    public async Task<bool> UpdateLineReceivedAsync(int lineId, int receivedQuantity)
    {
        var line = await _context.PurchaseOrderLines.FindAsync(lineId);
        if (line == null) return false;
        line.ReceivedQuantity += receivedQuantity;
        return await _context.SaveChangesAsync() > 0;
    }
}

public class AuditLogRepository : IAuditLogRepository
{
    private readonly PharmaceuticalDbContext _context;

    public AuditLogRepository(PharmaceuticalDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(AuditLogEntity log)
    {
        await _context.AuditLogs.AddAsync(log);
        await _context.SaveChangesAsync();
    }

    public async Task<List<AuditLogEntity>> GetRecentAsync(int count = 100) =>
        await _context.AuditLogs.OrderByDescending(a => a.CreatedAt).Take(count).ToListAsync();
}

public class AnalyticsRepository : IAnalyticsRepository
{
    private readonly PharmaceuticalDbContext _context;

    public AnalyticsRepository(PharmaceuticalDbContext context)
    {
        _context = context;
    }

    public async Task<decimal> GetTotalInventoryValueAsync() =>
        await _context.Drugs.Where(d => d.IsActive).SumAsync(d => d.StockQuantity * d.PurchasePrice);

    public async Task<int> GetActiveDrugCountAsync() =>
        await _context.Drugs.CountAsync(d => d.IsActive);

    public async Task<List<(string DrugId, string DrugName, int TotalOut)>> GetTopMovingDrugsAsync(int days, int top = 10)
    {
        var since = DateTime.UtcNow.AddDays(-days);
        var grouped = await _context.StockTransactions
            .Where(t => t.TransactionType == StockTransactionTypes.Out && t.CreatedAt >= since)
            .GroupBy(t => t.DrugId)
            .Select(g => new { DrugId = g.Key, TotalOut = g.Sum(x => x.Quantity) })
            .OrderByDescending(x => x.TotalOut)
            .Take(top)
            .ToListAsync();

        if (grouped.Count == 0) return new List<(string, string, int)>();

        var drugIds = grouped.Select(g => g.DrugId).ToList();
        var names = await _context.Drugs
            .Where(d => drugIds.Contains(d.DrugId))
            .Select(d => new { d.DrugId, d.DrugName })
            .ToDictionaryAsync(d => d.DrugId, d => d.DrugName);

        return grouped
            .Select(g => (g.DrugId, names.GetValueOrDefault(g.DrugId, g.DrugId), g.TotalOut))
            .ToList();
    }
}
