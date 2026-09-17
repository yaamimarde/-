using Microsoft.Extensions.Logging;
using Pharmaceutical.Core;
using Pharmaceutical.Core.DTOs;
using Pharmaceutical.Core.Interfaces;

namespace Pharmaceutical.Services;

public class SupplierService : ISupplierService
{
    private readonly ISupplierRepository _repository;
    private readonly ILogger<SupplierService> _logger;

    public SupplierService(ISupplierRepository repository, ILogger<SupplierService> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<List<SupplierDto>> GetAllAsync()
    {
        var suppliers = await _repository.GetAllAsync();
        return suppliers.Select(MapToDto).ToList();
    }

    public async Task<SupplierDto?> GetByIdAsync(int supplierId)
    {
        var supplier = await _repository.GetByIdAsync(supplierId);
        return supplier == null ? null : MapToDto(supplier);
    }

    public async Task<bool> AddAsync(SupplierCreateDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Name)) return false;

        var entity = new SupplierEntity
        {
            Name = dto.Name.Trim(),
            ContactPerson = dto.ContactPerson ?? string.Empty,
            Phone = dto.Phone ?? string.Empty,
            Address = dto.Address ?? string.Empty
        };

        try
        {
            return await _repository.AddAsync(entity);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "新增供应商失败");
            return false;
        }
    }

    public async Task<bool> UpdateAsync(int supplierId, SupplierUpdateDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Name)) return false;

        var entity = await _repository.GetByIdAsync(supplierId);
        if (entity == null) return false;

        entity.Name = dto.Name.Trim();
        entity.ContactPerson = dto.ContactPerson ?? string.Empty;
        entity.Phone = dto.Phone ?? string.Empty;
        entity.Address = dto.Address ?? string.Empty;

        try
        {
            return await _repository.UpdateAsync(entity);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新供应商失败: {SupplierId}", supplierId);
            return false;
        }
    }

    public async Task<bool> DeleteAsync(int supplierId)
    {
        try
        {
            return await _repository.DeleteAsync(supplierId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "删除供应商失败: {SupplierId}", supplierId);
            return false;
        }
    }

    private static SupplierDto MapToDto(SupplierEntity entity) => new()
    {
        SupplierId = entity.SupplierId,
        Name = entity.Name,
        ContactPerson = entity.ContactPerson,
        Phone = entity.Phone,
        Address = entity.Address
    };
}
