using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Pharmaceutical.Core;
using Pharmaceutical.Core.DTOs;
using Pharmaceutical.Core.Interfaces;

namespace Pharmaceutical.Services;

public class StockService : IStockService
{
    private readonly IStockRepository _repository;
    private readonly IDrugBatchRepository _batchRepository;
    private readonly IDistributedCache _cache;
    private readonly ILogger<StockService> _logger;
    private const string DrugCacheKey = "AllDrugs";

    public StockService(
        IStockRepository repository,
        IDrugBatchRepository batchRepository,
        IDistributedCache cache,
        ILogger<StockService> logger)
    {
        _repository = repository;
        _batchRepository = batchRepository;
        _cache = cache;
        _logger = logger;
    }

    public async Task<PagedResult<StockTransactionDto>> GetTransactionsAsync(StockTransactionQueryDto query)
    {
        query.Page = query.Page < 1 ? 1 : query.Page;
        query.PageSize = query.PageSize is < 1 or > 100 ? 20 : query.PageSize;

        var (items, total) = await _repository.GetPagedAsync(
            query.DrugId, query.TransactionType, query.FromDate, query.ToDate, query.Page, query.PageSize);

        return new PagedResult<StockTransactionDto>
        {
            Items = items.Select(MapToDto).ToList(),
            TotalCount = total,
            Page = query.Page,
            PageSize = query.PageSize
        };
    }

    public async Task<bool> StockInAsync(StockInDto dto, string operatorName)
    {
        if (string.IsNullOrWhiteSpace(dto.DrugId) || dto.Quantity <= 0) return false;

        try
        {
            var batchNumber = string.IsNullOrWhiteSpace(dto.BatchNumber)
                ? $"AUTO-{DateTime.UtcNow:yyyyMMddHHmmss}"
                : dto.BatchNumber.Trim();
            var expiry = dto.ExpiryDate ?? DateTime.UtcNow.Date.AddYears(2);

            await _batchRepository.AddOrUpdateBatchAsync(
                dto.DrugId, batchNumber, expiry, dto.ManufactureDate, dto.Quantity);

            var result = await _repository.ProcessTransactionAsync(
                dto.DrugId, StockTransactionTypes.In, dto.Quantity, operatorName, dto.Remark);
            if (result) await InvalidateCacheAsync();
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "入库失败: {DrugId}", dto.DrugId);
            return false;
        }
    }

    public async Task<bool> StockOutAsync(StockOutDto dto, string operatorName)
    {
        if (string.IsNullOrWhiteSpace(dto.DrugId) || dto.Quantity <= 0) return false;

        try
        {
            var result = await _repository.ProcessTransactionAsync(
                dto.DrugId, StockTransactionTypes.Out, dto.Quantity, operatorName, dto.Remark);
            if (result) await InvalidateCacheAsync();
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "出库失败: {DrugId}", dto.DrugId);
            return false;
        }
    }

    public async Task<bool> StockAdjustAsync(StockAdjustDto dto, string operatorName)
    {
        if (string.IsNullOrWhiteSpace(dto.DrugId) || dto.Quantity == 0) return false;

        try
        {
            var result = await _repository.ProcessTransactionAsync(
                dto.DrugId, StockTransactionTypes.Adjust, dto.Quantity, operatorName, dto.Remark);
            if (result) await InvalidateCacheAsync();
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "库存调整失败: {DrugId}", dto.DrugId);
            return false;
        }
    }

    public async Task<bool> StockReturnAsync(StockOutDto dto, string operatorName)
    {
        if (string.IsNullOrWhiteSpace(dto.DrugId) || dto.Quantity <= 0) return false;

        try
        {
            var result = await _repository.ProcessTransactionAsync(
                dto.DrugId, StockTransactionTypes.Return, dto.Quantity, operatorName, dto.Remark);
            if (result) await InvalidateCacheAsync();
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "退货入库失败: {DrugId}", dto.DrugId);
            return false;
        }
    }

    public async Task<bool> StockDamageAsync(StockOutDto dto, string operatorName)
    {
        if (string.IsNullOrWhiteSpace(dto.DrugId) || dto.Quantity <= 0) return false;

        try
        {
            var result = await _repository.ProcessTransactionAsync(
                dto.DrugId, StockTransactionTypes.Damage, dto.Quantity, operatorName, dto.Remark);
            if (result) await InvalidateCacheAsync();
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "报损失败: {DrugId}", dto.DrugId);
            return false;
        }
    }

    public async Task<List<DrugBatchDto>> GetBatchesAsync(string drugId)
    {
        var batches = await _batchRepository.GetByDrugIdAsync(drugId);
        var today = DateTime.UtcNow.Date;
        return batches.Select(b => new DrugBatchDto
        {
            BatchId = b.BatchId,
            DrugId = b.DrugId,
            DrugName = b.Drug?.DrugName ?? string.Empty,
            BatchNumber = b.BatchNumber,
            ManufactureDate = b.ManufactureDate,
            ExpiryDate = b.ExpiryDate,
            Quantity = b.Quantity,
            DaysUntilExpiry = (b.ExpiryDate.Date - today).Days
        }).ToList();
    }

    public async Task<List<ExpiryAlertDto>> GetExpiryAlertsAsync(int withinDays = 90)
    {
        var batches = await _batchRepository.GetExpiringAsync(withinDays);
        var today = DateTime.UtcNow.Date;
        return batches.Select(b =>
        {
            var days = (b.ExpiryDate.Date - today).Days;
            return new ExpiryAlertDto
            {
                BatchId = b.BatchId,
                DrugId = b.DrugId,
                DrugName = b.Drug?.DrugName ?? string.Empty,
                BatchNumber = b.BatchNumber,
                ExpiryDate = b.ExpiryDate,
                Quantity = b.Quantity,
                DaysUntilExpiry = days,
                AlertLevel = days <= 30 ? "紧急" : days <= 60 ? "警告" : "提醒"
            };
        }).ToList();
    }

    private async Task InvalidateCacheAsync()
    {
        try { await _cache.RemoveAsync(DrugCacheKey); }
        catch (Exception ex) { _logger.LogWarning(ex, "Redis cache clear failed"); }
    }

    private static StockTransactionDto MapToDto(StockTransactionEntity t) => new()
    {
        TransactionId = t.TransactionId,
        DrugId = t.DrugId,
        DrugName = t.Drug?.DrugName ?? string.Empty,
        TransactionType = t.TransactionType,
        Quantity = t.Quantity,
        Operator = t.Operator,
        CreatedAt = t.CreatedAt,
        Remark = t.Remark
    };
}
