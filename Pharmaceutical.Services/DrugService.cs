using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Pharmaceutical.Core;
using Pharmaceutical.Core.DTOs;
using Pharmaceutical.Core.Interfaces;
using System.Text.Json;

namespace Pharmaceutical.Services;

public class DrugService : IDrugService
{
    private readonly IDrugRepository _repository;
    private readonly IDistributedCache _cache;
    private readonly ILogger<DrugService> _logger;
    private const string CacheKeyPrefix = "DrugsPaged:";

    public DrugService(IDrugRepository repository, IDistributedCache cache, ILogger<DrugService> logger)
    {
        _repository = repository;
        _cache = cache;
        _logger = logger;
    }

    public async Task<PagedResult<DrugDto>> GetPagedAsync(string? search, int page, int pageSize, bool? activeOnly = true)
    {
        page = page < 1 ? 1 : page;
        pageSize = pageSize is < 1 or > 100 ? 20 : pageSize;

        var cacheKey = $"{CacheKeyPrefix}{search}:{page}:{pageSize}:{activeOnly}";
        try
        {
            var cached = await _cache.GetStringAsync(cacheKey);
            if (!string.IsNullOrEmpty(cached))
            {
                var cachedResult = JsonSerializer.Deserialize<PagedResult<DrugDto>>(cached);
                if (cachedResult != null) return cachedResult;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Redis read failed for drug paging");
        }

        var result = await _repository.GetPagedAsync(search, page, pageSize, activeOnly);
        var dto = new PagedResult<DrugDto>
        {
            Items = result.Items.Select(MapToDto).ToList(),
            TotalCount = result.TotalCount,
            Page = result.Page,
            PageSize = result.PageSize
        };

        try
        {
            await _cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(dto),
                new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5) });
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Redis write failed for drug paging");
        }

        return dto;
    }

    public async Task<DrugDto?> GetByIdAsync(string drugId)
    {
        var drug = await _repository.GetByIdAsync(drugId);
        return drug == null ? null : MapToDto(drug);
    }

    public async Task<List<DrugDto>> GetLowStockAsync(int threshold)
    {
        var drugs = await _repository.GetLowStockAsync(threshold);
        return drugs.Select(MapToDto).ToList();
    }

    public async Task<bool> AddAsync(DrugCreateDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.DrugId) || string.IsNullOrWhiteSpace(dto.DrugName))
            return false;

        var entity = new DrugCatalogEntity
        {
            DrugId = dto.DrugId.Trim(),
            DrugName = dto.DrugName.Trim(),
            TradeName = dto.TradeName ?? string.Empty,
            Specification = dto.Specification ?? string.Empty,
            DosageForm = dto.DosageForm ?? string.Empty,
            ApprovalNum = dto.ApprovalNum ?? string.Empty,
            StorageCond = dto.StorageCond ?? string.Empty,
            PurchasePrice = dto.PurchasePrice,
            RetailPrice = dto.RetailPrice,
            StockQuantity = dto.StockQuantity,
            SupplierId = dto.SupplierId,
            IsActive = true
        };

        try
        {
            var result = await _repository.AddAsync(entity);
            if (result) await InvalidateCacheAsync();
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "新增药品失败: {DrugId}", dto.DrugId);
            return false;
        }
    }

    public async Task<bool> UpdateAsync(string drugId, DrugUpdateDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.DrugName)) return false;

        var entity = await _repository.GetByIdAsync(drugId);
        if (entity == null) return false;

        entity.DrugName = dto.DrugName.Trim();
        entity.TradeName = dto.TradeName ?? string.Empty;
        entity.Specification = dto.Specification ?? string.Empty;
        entity.DosageForm = dto.DosageForm ?? string.Empty;
        entity.ApprovalNum = dto.ApprovalNum ?? string.Empty;
        entity.StorageCond = dto.StorageCond ?? string.Empty;
        entity.PurchasePrice = dto.PurchasePrice;
        entity.RetailPrice = dto.RetailPrice;
        entity.SupplierId = dto.SupplierId;

        try
        {
            var result = await _repository.UpdateAsync(entity);
            if (result) await InvalidateCacheAsync();
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新药品失败: {DrugId}", drugId);
            return false;
        }
    }

    public async Task<bool> DeactivateAsync(string drugId)
    {
        try
        {
            var result = await _repository.DeactivateAsync(drugId);
            if (result) await InvalidateCacheAsync();
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "下架药品失败: {DrugId}", drugId);
            return false;
        }
    }

    private async Task InvalidateCacheAsync()
    {
        try { await _cache.RemoveAsync("AllDrugs"); }
        catch (Exception ex) { _logger.LogWarning(ex, "Redis cache clear failed"); }
    }

    private static DrugDto MapToDto(DrugCatalogEntity entity) => new()
    {
        DrugId = entity.DrugId,
        DrugName = entity.DrugName,
        TradeName = entity.TradeName,
        Specification = entity.Specification,
        DosageForm = entity.DosageForm,
        ApprovalNum = entity.ApprovalNum,
        StorageCond = entity.StorageCond,
        PurchasePrice = entity.PurchasePrice,
        RetailPrice = entity.RetailPrice,
        StockQuantity = entity.StockQuantity,
        SupplierId = entity.SupplierId,
        SupplierName = entity.Supplier?.Name ?? string.Empty,
        IsActive = entity.IsActive
    };
}
