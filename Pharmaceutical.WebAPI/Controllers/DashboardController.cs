using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Pharmaceutical.Core.DTOs;
using Pharmaceutical.Core.Interfaces;
using Pharmaceutical.Core.Settings;

namespace Pharmaceutical.WebAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class DashboardController : ControllerBase
{
    private readonly IAnalyticsService _analyticsService;
    private readonly AlertSettings _alertSettings;

    public DashboardController(IAnalyticsService analyticsService, IOptions<AlertSettings> alertSettings)
    {
        _analyticsService = analyticsService;
        _alertSettings = alertSettings.Value;
    }

    [HttpGet]
    public async Task<IActionResult> GetDashboard([FromQuery] int? expiryDays)
    {
        var dashboard = await _analyticsService.GetDashboardAsync(
            _alertSettings.LowStockThreshold, expiryDays ?? 90);
        return Ok(ApiResponse<DashboardDto>.Ok(dashboard));
    }
}

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class AuditController : ControllerBase
{
    private readonly IAuditService _auditService;

    public AuditController(IAuditService auditService)
    {
        _auditService = auditService;
    }

    [HttpGet]
    public async Task<IActionResult> GetRecent([FromQuery] int count = 100)
    {
        var logs = await _auditService.GetRecentAsync(count);
        return Ok(ApiResponse<List<AuditLogDto>>.Ok(logs));
    }
}

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ExportController : ControllerBase
{
    private readonly IExportService _exportService;
    private readonly AlertSettings _alertSettings;

    public ExportController(IExportService exportService, IOptions<AlertSettings> alertSettings)
    {
        _exportService = exportService;
        _alertSettings = alertSettings.Value;
    }

    [HttpGet("drugs")]
    public async Task<IActionResult> ExportDrugs()
    {
        var bytes = await _exportService.ExportDrugsCsvAsync();
        return File(bytes, "text/csv", "drugs.csv");
    }

    [HttpGet("transactions")]
    public async Task<IActionResult> ExportTransactions([FromQuery] StockTransactionQueryDto query)
    {
        var bytes = await _exportService.ExportTransactionsCsvAsync(query);
        return File(bytes, "text/csv", "transactions.csv");
    }

    [HttpGet("low-stock")]
    public async Task<IActionResult> ExportLowStock()
    {
        var bytes = await _exportService.ExportLowStockCsvAsync(_alertSettings.LowStockThreshold);
        return File(bytes, "text/csv", "low-stock.csv");
    }

    [HttpGet("expiry-alerts")]
    public async Task<IActionResult> ExportExpiryAlerts([FromQuery] int withinDays = 90)
    {
        var bytes = await _exportService.ExportExpiryAlertsCsvAsync(withinDays);
        return File(bytes, "text/csv", "expiry-alerts.csv");
    }
}
