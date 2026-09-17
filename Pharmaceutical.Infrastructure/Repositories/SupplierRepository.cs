using Microsoft.EntityFrameworkCore;
using Pharmaceutical.Core;
using Pharmaceutical.Core.Interfaces;

namespace Pharmaceutical.Infrastructure.Repositories;

public class SupplierRepository : ISupplierRepository
{
    private readonly PharmaceuticalDbContext _context;

    public SupplierRepository(PharmaceuticalDbContext context)
    {
        _context = context;
    }

    public async Task<List<SupplierEntity>> GetAllAsync() =>
        await _context.Suppliers.OrderBy(s => s.SupplierId).ToListAsync();

    public async Task<SupplierEntity?> GetByIdAsync(int supplierId) =>
        await _context.Suppliers.FirstOrDefaultAsync(s => s.SupplierId == supplierId);

    public async Task<bool> AddAsync(SupplierEntity supplier)
    {
        await _context.Suppliers.AddAsync(supplier);
        return await _context.SaveChangesAsync() > 0;
    }

    public async Task<bool> UpdateAsync(SupplierEntity supplier)
    {
        _context.Suppliers.Update(supplier);
        return await _context.SaveChangesAsync() > 0;
    }

    public async Task<bool> DeleteAsync(int supplierId)
    {
        var supplier = await GetByIdAsync(supplierId);
        if (supplier == null) return false;

        // Prevent deletion if supplier has associated drugs
        var hasDrugs = await _context.Drugs.AnyAsync(d => d.SupplierId == supplierId);
        if (hasDrugs)
            throw new InvalidOperationException("该供应商下存在关联药品，无法删除。请先迁移药品数据。");

        _context.Suppliers.Remove(supplier);
        return await _context.SaveChangesAsync() > 0;
    }
}
