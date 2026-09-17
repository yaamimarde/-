using Pharmaceutical.Core.DTOs;

namespace Pharmaceutical.Core.Interfaces;

public interface ISupplierService
{
    Task<List<SupplierDto>> GetAllAsync();
    Task<SupplierDto?> GetByIdAsync(int supplierId);
    Task<bool> AddAsync(SupplierCreateDto dto);
    Task<bool> UpdateAsync(int supplierId, SupplierUpdateDto dto);
    Task<bool> DeleteAsync(int supplierId);
}
