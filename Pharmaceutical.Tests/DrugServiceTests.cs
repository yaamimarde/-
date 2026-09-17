using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Pharmaceutical.Core;
using Pharmaceutical.Core.DTOs;
using Pharmaceutical.Core.Interfaces;
using Pharmaceutical.Services;

namespace Pharmaceutical.Tests;

public class DrugServiceTests
{
    private readonly Mock<IDrugRepository> _repositoryMock = new();
    private readonly Mock<IDistributedCache> _cacheMock = new();
    private readonly DrugService _service;

    public DrugServiceTests()
    {
        _service = new DrugService(
            _repositoryMock.Object,
            _cacheMock.Object,
            NullLogger<DrugService>.Instance);
    }

    [Fact]
    public async Task AddAsync_ClearsCache_OnSuccess()
    {
        var dto = new DrugCreateDto { DrugId = "D001", DrugName = "测试药品" };
        _repositoryMock.Setup(r => r.AddAsync(It.IsAny<DrugCatalogEntity>())).ReturnsAsync(true);

        var result = await _service.AddAsync(dto);

        Assert.True(result);
    }

    [Fact]
    public async Task DeactivateAsync_ReturnsTrue_WhenExists()
    {
        _repositoryMock.Setup(r => r.DeactivateAsync("D001")).ReturnsAsync(true);

        var result = await _service.DeactivateAsync("D001");

        Assert.True(result);
    }

    [Fact]
    public async Task AddAsync_ReturnsFalse_WhenNameEmpty()
    {
        var dto = new DrugCreateDto { DrugId = "D001", DrugName = "" };
        var result = await _service.AddAsync(dto);
        Assert.False(result);
    }

    [Fact]
    public async Task UpdateAsync_DoesNotChangeStock()
    {
        var entity = new DrugCatalogEntity { DrugId = "D001", DrugName = "旧名", StockQuantity = 100 };
        _repositoryMock.Setup(r => r.GetByIdAsync("D001")).ReturnsAsync(entity);
        _repositoryMock.Setup(r => r.UpdateAsync(It.IsAny<DrugCatalogEntity>())).ReturnsAsync(true);

        await _service.UpdateAsync("D001", new DrugUpdateDto { DrugName = "新名", SupplierId = 1 });

        _repositoryMock.Verify(r => r.UpdateAsync(It.Is<DrugCatalogEntity>(e => e.StockQuantity == 100)), Times.Once);
    }
}

public class StockServiceTests
{
    [Fact]
    public async Task StockOut_ReturnsFalse_WhenQuantityInvalid()
    {
        var stockRepo = new Mock<IStockRepository>();
        var batchRepo = new Mock<IDrugBatchRepository>();
        var cache = new Mock<IDistributedCache>();
        var service = new StockService(stockRepo.Object, batchRepo.Object, cache.Object, NullLogger<StockService>.Instance);

        var result = await service.StockOutAsync(new StockOutDto { DrugId = "D1", Quantity = 0 }, "admin");
        Assert.False(result);
    }
}
