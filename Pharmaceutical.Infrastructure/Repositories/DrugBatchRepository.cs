using Microsoft.EntityFrameworkCore;
using Pharmaceutical.Core;
using Pharmaceutical.Core.Interfaces;

namespace Pharmaceutical.Infrastructure.Repositories;

public class DrugBatchRepository : IDrugBatchRepository
{
    private readonly PharmaceuticalDbContext _context;

    public DrugBatchRepository(PharmaceuticalDbContext context)
    {
        _context = context;
    }

    public async Task<List<DrugBatchEntity>> GetByDrugIdAsync(string drugId) =>
        await _context.DrugBatches
            .Include(b => b.Drug)
            .Where(b => b.DrugId == drugId && b.Quantity > 0)
            .OrderBy(b => b.ExpiryDate)
            .ToListAsync();

    public async Task<List<DrugBatchEntity>> GetExpiringAsync(int withinDays)
    {
        var deadline = DateTime.UtcNow.Date.AddDays(withinDays);
        return await _context.DrugBatches
            .Include(b => b.Drug)
            .Where(b => b.Quantity > 0 && b.ExpiryDate <= deadline && b.Drug!.IsActive)
            .OrderBy(b => b.ExpiryDate)
            .ToListAsync();
    }

    public async Task<DrugBatchEntity?> GetByIdAsync(int batchId) =>
        await _context.DrugBatches.Include(b => b.Drug).FirstOrDefaultAsync(b => b.BatchId == batchId);

    public async Task AddOrUpdateBatchAsync(string drugId, string batchNumber, DateTime expiryDate, DateTime? manufactureDate, int quantity)
    {
        var batch = await _context.DrugBatches
            .FirstOrDefaultAsync(b => b.DrugId == drugId && b.BatchNumber == batchNumber);

        if (batch == null)
        {
            await _context.DrugBatches.AddAsync(new DrugBatchEntity
            {
                DrugId = drugId,
                BatchNumber = batchNumber,
                ManufactureDate = manufactureDate,
                ExpiryDate = expiryDate,
                Quantity = quantity
            });
        }
        else
        {
            batch.Quantity += quantity;
            batch.ExpiryDate = expiryDate;
            batch.ManufactureDate = manufactureDate ?? batch.ManufactureDate;
        }

        await _context.SaveChangesAsync();
    }

    public async Task<bool> DeductFefoAsync(string drugId, int quantity)
    {
        var batches = await _context.DrugBatches
            .Where(b => b.DrugId == drugId && b.Quantity > 0)
            .OrderBy(b => b.ExpiryDate)
            .ToListAsync();

        if (batches.Count == 0)
            return true;

        var remaining = quantity;
        foreach (var batch in batches)
        {
            if (remaining <= 0) break;
            var deduct = Math.Min(batch.Quantity, remaining);
            batch.Quantity -= deduct;
            remaining -= deduct;
        }

        if (remaining > 0) return false;

        await _context.SaveChangesAsync();
        return true;
    }
}
