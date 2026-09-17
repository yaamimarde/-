using Microsoft.EntityFrameworkCore;
using Pharmaceutical.Core;
using Pharmaceutical.Core.DTOs;
using Pharmaceutical.Core.Interfaces;

namespace Pharmaceutical.Infrastructure.Repositories;

public class DrugRepository : IDrugRepository
{
    private readonly PharmaceuticalDbContext _context;

    public DrugRepository(PharmaceuticalDbContext context)
    {
        _context = context;
    }

    public async Task<PagedResult<DrugCatalogEntity>> GetPagedAsync(string? search, int page, int pageSize, bool? activeOnly = true)
    {
        var query = _context.Drugs.Include(d => d.Supplier).AsQueryable();

        if (activeOnly == true)
            query = query.Where(d => d.IsActive);
        else if (activeOnly == false)
            query = query.Where(d => !d.IsActive);

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(d =>
                d.DrugName.Contains(search) ||
                d.DrugId.Contains(search) ||
                (d.TradeName != null && d.TradeName.Contains(search)));
        }

        var totalCount = await query.CountAsync();
        var items = await query
            .OrderBy(d => d.DrugId)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedResult<DrugCatalogEntity>
        {
            Items = items,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<DrugCatalogEntity?> GetByIdAsync(string drugId) =>
        await _context.Drugs.Include(d => d.Supplier).FirstOrDefaultAsync(d => d.DrugId == drugId);

    public async Task<List<DrugCatalogEntity>> GetLowStockAsync(int threshold) =>
        await _context.Drugs
            .Include(d => d.Supplier)
            .Where(d => d.IsActive && d.StockQuantity < threshold)
            .OrderBy(d => d.StockQuantity)
            .ToListAsync();

    public async Task<bool> AddAsync(DrugCatalogEntity drug)
    {
        var exists = await _context.Drugs.AnyAsync(d => d.DrugId == drug.DrugId);
        if (exists) return false;

        await _context.Drugs.AddAsync(drug);
        return await _context.SaveChangesAsync() > 0;
    }

    public async Task<bool> UpdateAsync(DrugCatalogEntity drug)
    {
        _context.Drugs.Update(drug);
        return await _context.SaveChangesAsync() > 0;
    }

    public async Task<bool> DeactivateAsync(string drugId)
    {
        var drug = await GetByIdAsync(drugId);
        if (drug == null || !drug.IsActive) return false;

        drug.IsActive = false;
        return await _context.SaveChangesAsync() > 0;
    }
}
