namespace Pharmaceutical.Core.Interfaces;

public interface ISupplierRepository
{
    Task<List<SupplierEntity>> GetAllAsync();
    Task<SupplierEntity?> GetByIdAsync(int supplierId);
    Task<bool> AddAsync(SupplierEntity supplier);
    Task<bool> UpdateAsync(SupplierEntity supplier);
    Task<bool> DeleteAsync(int supplierId);
}
