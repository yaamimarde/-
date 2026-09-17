using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pharmaceutical.Core.DTOs;
using Pharmaceutical.Core.Interfaces;

namespace Pharmaceutical.WebAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class SupplierController : ControllerBase
{
    private readonly ISupplierService _supplierService;

    public SupplierController(ISupplierService supplierService)
    {
        _supplierService = supplierService;
    }

    [HttpGet]
    public async Task<IActionResult> GetSuppliers()
    {
        var suppliers = await _supplierService.GetAllAsync();
        return Ok(ApiResponse<List<SupplierDto>>.Ok(suppliers));
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetSupplier(int id)
    {
        var supplier = await _supplierService.GetByIdAsync(id);
        if (supplier == null) return NotFound(ApiResponse<SupplierDto>.Fail($"未找到供应商 {id}"));
        return Ok(ApiResponse<SupplierDto>.Ok(supplier));
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> CreateSupplier([FromBody] SupplierCreateDto model)
    {
        var result = await _supplierService.AddAsync(model);
        if (!result) return BadRequest(ApiResponse<object>.Fail("供应商创建失败"));
        return Ok(ApiResponse<object>.Ok(null, "供应商创建成功"));
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateSupplier(int id, [FromBody] SupplierUpdateDto model)
    {
        var result = await _supplierService.UpdateAsync(id, model);
        if (!result) return BadRequest(ApiResponse<object>.Fail("供应商更新失败"));
        return Ok(ApiResponse<object>.Ok(null, "供应商更新成功"));
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteSupplier(int id)
    {
        try
        {
            var result = await _supplierService.DeleteAsync(id);
            if (!result) return NotFound(ApiResponse<object>.Fail($"未找到供应商 {id}"));
            return Ok(ApiResponse<object>.Ok(null, "供应商删除成功"));
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(ApiResponse<object>.Fail(ex.Message));
        }
    }
}
