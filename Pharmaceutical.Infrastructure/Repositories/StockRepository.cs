using Microsoft.EntityFrameworkCore;
using Pharmaceutical.Core;
using Pharmaceutical.Core.Interfaces;

namespace Pharmaceutical.Infrastructure.Repositories;

public class StockRepository : IStockRepository
{
    private readonly PharmaceuticalDbContext _context;
    private readonly IDrugBatchRepository _batchRepository;

    public StockRepository(PharmaceuticalDbContext context, IDrugBatchRepository batchRepository)
    {
        _context = context;
        _batchRepository = batchRepository;
    }

    public async Task<(List<StockTransactionEntity> Items, int TotalCount)> GetPagedAsync(
        string? drugId, string? transactionType, DateTime? fromDate, DateTime? toDate, int page, int pageSize)
    {
        var query = _context.StockTransactions.Include(t => t.Drug).AsQueryable();

        if (!string.IsNullOrWhiteSpace(drugId))
            query = query.Where(t => t.DrugId == drugId);
        if (!string.IsNullOrWhiteSpace(transactionType))
            query = query.Where(t => t.TransactionType == transactionType);
        if (fromDate.HasValue)
            query = query.Where(t => t.CreatedAt >= fromDate.Value);
        if (toDate.HasValue)
            query = query.Where(t => t.CreatedAt <= toDate.Value.AddDays(1));

        var totalCount = await query.CountAsync();
        var items = await query
            .OrderByDescending(t => t.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, totalCount);
    }

    public async Task<bool> ProcessTransactionAsync(string drugId, string transactionType, int quantity, string operatorName, string? remark)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync();

        var drug = await _context.Drugs
            .FromSqlRaw("SELECT * FROM drugs WHERE drug_id = {0} FOR UPDATE", drugId)
            .FirstOrDefaultAsync();
        if (drug == null || !drug.IsActive) return false;

        if (transactionType == StockTransactionTypes.In)
        {
            drug.StockQuantity += quantity;
        }
        else if (transactionType == StockTransactionTypes.Out)
        {
            if (drug.StockQuantity < quantity) return false;
            if (!await _batchRepository.DeductFefoAsync(drugId, quantity)) return false;
            drug.StockQuantity -= quantity;
        }
        else if (transactionType == StockTransactionTypes.Return)
        {
            drug.StockQuantity += quantity;
        }
        else if (transactionType == StockTransactionTypes.Damage)
        {
            if (drug.StockQuantity < quantity) return false;
            if (!await _batchRepository.DeductFefoAsync(drugId, quantity)) return false;
            drug.StockQuantity -= quantity;
        }
        else if (transactionType == StockTransactionTypes.Adjust)
        {
            drug.StockQuantity += quantity;
        }
        else
        {
            return false;
        }

        await _context.StockTransactions.AddAsync(new StockTransactionEntity
        {
            DrugId = drugId,
            TransactionType = transactionType,
            Quantity = Math.Abs(quantity),
            Operator = operatorName,
            CreatedAt = DateTime.UtcNow,
            Remark = remark
        });

        await _context.SaveChangesAsync();
        await transaction.CommitAsync();
        return true;
    }
}
