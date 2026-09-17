using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pharmaceutical.Core.DTOs;
using Pharmaceutical.Core.Interfaces;
using System.Security.Claims;

namespace Pharmaceutical.WebAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PurchaseOrderController : ControllerBase
{
    private readonly IPurchaseOrderService _service;

    public PurchaseOrderController(IPurchaseOrderService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var orders = await _service.GetAllAsync();
        return Ok(ApiResponse<List<PurchaseOrderDto>>.Ok(orders));
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var order = await _service.GetByIdAsync(id);
        if (order == null) return NotFound(ApiResponse<object>.Fail("采购单不存在"));
        return Ok(ApiResponse<PurchaseOrderDto>.Ok(order));
    }

    [HttpPost]
    [Authorize(Roles = "Admin,Operator")]
    public async Task<IActionResult> Create([FromBody] CreatePurchaseOrderDto dto)
    {
        var user = User.FindFirstValue(ClaimTypes.Name) ?? "unknown";
        var id = await _service.CreateAsync(dto, user);
        return Ok(ApiResponse<object>.Ok(new { orderId = id }, "采购单创建成功"));
    }

    [HttpPost("from-low-stock")]
    [Authorize(Roles = "Admin,Operator")]
    public async Task<IActionResult> CreateFromLowStock([FromQuery] int supplierId, [FromQuery] int? threshold)
    {
        var user = User.FindFirstValue(ClaimTypes.Name) ?? "unknown";
        var id = await _service.CreateFromLowStockAsync(supplierId, threshold ?? 200, user);
        if (id == 0) return BadRequest(ApiResponse<object>.Fail("该供应商无低库存药品"));
        return Ok(ApiResponse<object>.Ok(new { orderId = id }, "已从低库存生成采购单"));
    }

    [HttpPost("{id}/submit")]
    [Authorize(Roles = "Admin,Operator")]
    public async Task<IActionResult> Submit(int id)
    {
        var ok = await _service.SubmitAsync(id);
        return ok ? Ok(ApiResponse<object>.Ok(null, "已提交审核")) : BadRequest(ApiResponse<object>.Fail("提交失败"));
    }

    [HttpPost("{id}/approve")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Approve(int id)
    {
        var ok = await _service.ApproveAsync(id);
        return ok ? Ok(ApiResponse<object>.Ok(null, "已审核通过")) : BadRequest(ApiResponse<object>.Fail("审核失败"));
    }

    [HttpPost("{id}/receive")]
    [Authorize(Roles = "Admin,Operator")]
    public async Task<IActionResult> Receive(int id, [FromBody] ReceivePurchaseOrderDto? options)
    {
        var user = User.FindFirstValue(ClaimTypes.Name) ?? "unknown";
        var ok = await _service.ReceiveAsync(id, user, options);
        return ok ? Ok(ApiResponse<object>.Ok(null, "收货入库成功")) : BadRequest(ApiResponse<object>.Fail("收货失败"));
    }

    [HttpPost("{id}/cancel")]
    [Authorize(Roles = "Admin,Operator")]
    public async Task<IActionResult> Cancel(int id)
    {
        var ok = await _service.CancelAsync(id);
        return ok ? Ok(ApiResponse<object>.Ok(null, "采购单已取消")) : BadRequest(ApiResponse<object>.Fail("取消失败，仅草稿或待审核状态可取消"));
    }
}
