using System.Net.Http.Json;
using Pharmaceutical.Core.DTOs;
using Pharmaceutical.Core.Settings;

namespace Pharmaceutical.Blazor.Services;

public class AuthApiService
{
    private readonly HttpClient _httpClient;
    private readonly ScopedApiHttpClient _api;
    private readonly AuthStateService _authState;

    public AuthApiService(HttpClient httpClient, ScopedApiHttpClient api, AuthStateService authState)
    {
        _httpClient = httpClient;
        _api = api;
        _authState = authState;
    }

    public async Task<(bool Success, string Message)> LoginAsync(string username, string password)
    {
        var response = await _httpClient.PostAsJsonAsync("api/auth/login", new LoginDto
        {
            Username = username,
            Password = password
        });

        var (success, data, message) = await ApiClientHelper.GetApiData<LoginResponseDto>(response);
        if (success && data != null)
        {
            await _authState.SetAuthAsync(data);
            return (true, message);
        }

        return (false, message ?? "登录失败");
    }

    public async Task LogoutAsync() => await _authState.ClearAsync();

    public async Task<(bool Success, string Message)> RegisterAsync(RegisterDto dto)
    {
        var response = await _httpClient.PostAsJsonAsync("api/auth/register", dto);
        var (success, _, message) = await ApiClientHelper.GetApiData<object>(response);
        return (success, message ?? (success ? "用户创建成功" : "创建失败"));
    }

    public async Task<List<UserDto>> GetUsersAsync()
    {
        var response = await _api.GetAsync("api/users");
        var (success, data, _) = await ApiClientHelper.GetApiData<List<UserDto>>(response);
        return success ? data ?? new() : new();
    }

    public async Task<(bool Success, string Message)> UpdateRoleAsync(string id, string role)
    {
        var response = await _api.PutAsJsonAsync($"api/users/{id}/role", new UpdateUserRoleDto { Role = role });
        var (success, _, message) = await ApiClientHelper.GetApiData<object>(response);
        return (success, message ?? (success ? "角色已更新" : "更新失败"));
    }

    public async Task<(bool Success, string Message)> DeleteUserAsync(string id)
    {
        var response = await _api.DeleteAsync($"api/users/{id}");
        var (success, _, message) = await ApiClientHelper.GetApiData<object>(response);
        return (success, message ?? (success ? "用户已删除" : "删除失败"));
    }

    public async Task<(bool Success, string Message)> ResetPasswordAsync(string id, string newPassword)
    {
        var response = await _api.PostAsJsonAsync($"api/users/{id}/reset-password", new ResetPasswordDto { NewPassword = newPassword });
        var (success, _, message) = await ApiClientHelper.GetApiData<object>(response);
        return (success, message ?? (success ? "密码已重置" : "重置失败"));
    }
}

public class DrugApiService
{
    private readonly ScopedApiHttpClient _api;

    public DrugApiService(ScopedApiHttpClient api) => _api = api;

    public async Task<(PagedResult<DrugDto>? Data, string? Error)> GetPagedAsync(string? search, int page, int pageSize, bool? activeOnly = true)
    {
        var url = $"api/drug?page={page}&pageSize={pageSize}&activeOnly={activeOnly}";
        if (!string.IsNullOrWhiteSpace(search))
            url += $"&search={Uri.EscapeDataString(search)}";

        var response = await _api.GetAsync(url);
        var (success, data, message) = await ApiClientHelper.GetApiData<PagedResult<DrugDto>>(response);
        return success ? (data, null) : (null, message);
    }

    public async Task<List<DrugDto>> GetLowStockDrugsAsync(int? threshold = null)
    {
        var url = threshold.HasValue ? $"api/drug/low-stock?threshold={threshold.Value}" : "api/drug/low-stock";
        var response = await _api.GetAsync(url);
        var (success, data, _) = await ApiClientHelper.GetApiData<List<DrugDto>>(response);
        return success ? data ?? new() : new();
    }

    public async Task<AlertSettings?> GetAlertSettingsAsync()
    {
        var response = await _api.GetAsync("api/drug/alert-settings");
        var (success, data, _) = await ApiClientHelper.GetApiData<AlertSettings>(response);
        return success ? data : null;
    }

    public async Task<(bool Success, string Message)> AddDrugAsync(DrugCreateDto drug)
    {
        var response = await _api.PostAsJsonAsync("api/drug", drug);
        var (success, _, message) = await ApiClientHelper.GetApiData<object>(response);
        return (success, message ?? (success ? "录入成功" : "录入失败"));
    }

    public async Task<(bool Success, string Message)> UpdateDrugAsync(string drugId, DrugUpdateDto drug)
    {
        var response = await _api.PutAsJsonAsync($"api/drug/{Uri.EscapeDataString(drugId)}", drug);
        var (success, _, message) = await ApiClientHelper.GetApiData<object>(response);
        return (success, message ?? (success ? "更新成功" : "更新失败"));
    }

    public async Task<(bool Success, string Message)> DeleteDrugAsync(string drugId)
    {
        var response = await _api.DeleteAsync($"api/drug/{Uri.EscapeDataString(drugId)}");
        var (success, _, message) = await ApiClientHelper.GetApiData<object>(response);
        return (success, message ?? (success ? "下架成功" : "下架失败"));
    }
}

public class SupplierApiService
{
    private readonly ScopedApiHttpClient _api;

    public SupplierApiService(ScopedApiHttpClient api) => _api = api;

    public async Task<List<SupplierDto>> GetAllAsync()
    {
        var response = await _api.GetAsync("api/supplier");
        var (success, data, _) = await ApiClientHelper.GetApiData<List<SupplierDto>>(response);
        return success ? data ?? new() : new();
    }

    public async Task<(bool Success, string Message)> AddAsync(SupplierCreateDto dto)
    {
        var response = await _api.PostAsJsonAsync("api/supplier", dto);
        var (success, _, message) = await ApiClientHelper.GetApiData<object>(response);
        return (success, message ?? (success ? "创建成功" : "创建失败"));
    }

    public async Task<(bool Success, string Message)> UpdateAsync(int id, SupplierUpdateDto dto)
    {
        var response = await _api.PutAsJsonAsync($"api/supplier/{id}", dto);
        var (success, _, message) = await ApiClientHelper.GetApiData<object>(response);
        return (success, message ?? (success ? "更新成功" : "更新失败"));
    }

    public async Task<(bool Success, string Message)> DeleteAsync(int id)
    {
        var response = await _api.DeleteAsync($"api/supplier/{id}");
        var (success, _, message) = await ApiClientHelper.GetApiData<object>(response);
        return (success, message ?? (success ? "删除成功" : "删除失败"));
    }
}

public class StockApiService
{
    private readonly ScopedApiHttpClient _api;

    public StockApiService(ScopedApiHttpClient api) => _api = api;

    public async Task<(PagedResult<StockTransactionDto>? Data, string? Error)> GetTransactionsAsync(StockTransactionQueryDto query)
    {
        var url = $"api/stock?page={query.Page}&pageSize={query.PageSize}";
        if (!string.IsNullOrWhiteSpace(query.DrugId)) url += $"&drugId={Uri.EscapeDataString(query.DrugId)}";
        if (!string.IsNullOrWhiteSpace(query.TransactionType)) url += $"&transactionType={query.TransactionType}";
        var response = await _api.GetAsync(url);
        var (success, data, message) = await ApiClientHelper.GetApiData<PagedResult<StockTransactionDto>>(response);
        return success ? (data, null) : (null, message);
    }

    public async Task<(List<ExpiryAlertDto> Data, string? Error)> GetExpiryAlertsAsync(int withinDays = 90)
    {
        try
        {
            var response = await _api.GetAsync($"api/stock/expiry-alerts?withinDays={withinDays}");
            var (success, data, message) = await ApiClientHelper.GetApiData<List<ExpiryAlertDto>>(response);
            return success ? (data ?? new(), null) : (new(), message ?? "加载效期预警失败");
        }
        catch (Exception ex)
        {
            return (new(), $"无法连接 API：{ex.Message}");
        }
    }

    public async Task<(bool Success, string Message)> StockInAsync(StockInDto dto)
    {
        var response = await _api.PostAsJsonAsync("api/stock/in", dto);
        var (success, _, message) = await ApiClientHelper.GetApiData<object>(response);
        return (success, message ?? (success ? "入库成功" : "入库失败"));
    }

    public async Task<(bool Success, string Message)> StockOutAsync(StockOutDto dto)
    {
        var response = await _api.PostAsJsonAsync("api/stock/out", dto);
        var (success, _, message) = await ApiClientHelper.GetApiData<object>(response);
        return (success, message ?? (success ? "出库成功" : "出库失败"));
    }

    public async Task<List<DrugBatchDto>> GetBatchesAsync(string drugId)
    {
        var response = await _api.GetAsync($"api/stock/batches/{Uri.EscapeDataString(drugId)}");
        var (success, data, _) = await ApiClientHelper.GetApiData<List<DrugBatchDto>>(response);
        return success ? data ?? new() : new();
    }

    public async Task<(bool Success, string Message)> StockAdjustAsync(StockAdjustDto dto)
    {
        var response = await _api.PostAsJsonAsync("api/stock/adjust", dto);
        var (success, _, message) = await ApiClientHelper.GetApiData<object>(response);
        return (success, message ?? (success ? "调整成功" : "调整失败"));
    }

    public async Task<(bool Success, string Message)> StockReturnAsync(StockOutDto dto)
    {
        var response = await _api.PostAsJsonAsync("api/stock/return", dto);
        var (success, _, message) = await ApiClientHelper.GetApiData<object>(response);
        return (success, message ?? (success ? "退货入库成功" : "退货入库失败"));
    }

    public async Task<(bool Success, string Message)> StockDamageAsync(StockOutDto dto)
    {
        var response = await _api.PostAsJsonAsync("api/stock/damage", dto);
        var (success, _, message) = await ApiClientHelper.GetApiData<object>(response);
        return (success, message ?? (success ? "报损成功" : "报损失败"));
    }
}

public class DashboardApiService
{
    private readonly ScopedApiHttpClient _api;

    public DashboardApiService(ScopedApiHttpClient api) => _api = api;

    public async Task<(DashboardDto? Data, string? Error)> GetDashboardAsync(CancellationToken cancellationToken = default)
    {
        var response = await _api.GetAsync("api/dashboard", cancellationToken);
        var (success, data, message) = await ApiClientHelper.GetApiData<DashboardDto>(response);
        return success ? (data, null) : (null, message);
    }
}

public class PurchaseOrderApiService
{
    private readonly ScopedApiHttpClient _api;

    public PurchaseOrderApiService(ScopedApiHttpClient api) => _api = api;

    public async Task<List<PurchaseOrderDto>> GetAllAsync()
    {
        var response = await _api.GetAsync("api/purchaseorder");
        var (success, data, _) = await ApiClientHelper.GetApiData<List<PurchaseOrderDto>>(response);
        return success ? data ?? new() : new();
    }

    public async Task<(bool Success, string Message)> CreateFromLowStockAsync(int supplierId)
    {
        var response = await _api.PostAsync($"api/purchaseorder/from-low-stock?supplierId={supplierId}", null);
        var (success, _, message) = await ApiClientHelper.GetApiData<object>(response);
        return (success, message ?? "");
    }

    public async Task<(bool Success, string Message)> CreateAsync(CreatePurchaseOrderDto dto)
    {
        var response = await _api.PostAsJsonAsync("api/purchaseorder", dto);
        var (success, _, message) = await ApiClientHelper.GetApiData<object>(response);
        return (success, message ?? (success ? "采购单创建成功" : "创建失败"));
    }

    public async Task<(bool Success, string Message)> SubmitAsync(int id)
    {
        var response = await _api.PostAsync($"api/purchaseorder/{id}/submit", null);
        var (success, _, message) = await ApiClientHelper.GetApiData<object>(response);
        return (success, message ?? (success ? "已提交审核" : "提交失败"));
    }

    public async Task<(bool Success, string Message)> ApproveAsync(int id)
    {
        var response = await _api.PostAsync($"api/purchaseorder/{id}/approve", null);
        var (success, _, message) = await ApiClientHelper.GetApiData<object>(response);
        return (success, message ?? (success ? "已审核通过" : "审核失败"));
    }

    public async Task<(bool Success, string Message)> ReceiveAsync(int id)
    {
        var response = await _api.PostAsJsonAsync($"api/purchaseorder/{id}/receive", new ReceivePurchaseOrderDto());
        var (success, _, message) = await ApiClientHelper.GetApiData<object>(response);
        return (success, message ?? (success ? "收货入库成功" : "收货失败"));
    }

    public async Task<(bool Success, string Message)> CancelAsync(int id)
    {
        var response = await _api.PostAsync($"api/purchaseorder/{id}/cancel", null);
        var (success, _, message) = await ApiClientHelper.GetApiData<object>(response);
        return (success, message ?? (success ? "已取消" : "取消失败"));
    }
}

public class ExportApiService
{
    private readonly ScopedApiHttpClient _api;

    public ExportApiService(ScopedApiHttpClient api) => _api = api;

    public async Task<byte[]?> DownloadDrugsAsync()
    {
        var response = await _api.GetAsync("api/export/drugs");
        return response.IsSuccessStatusCode ? await response.Content.ReadAsByteArrayAsync() : null;
    }

    public async Task<byte[]?> DownloadTransactionsAsync(StockTransactionQueryDto query)
    {
        var url = $"api/export/transactions?page={query.Page}&pageSize={query.PageSize}";
        if (!string.IsNullOrWhiteSpace(query.DrugId)) url += $"&drugId={Uri.EscapeDataString(query.DrugId)}";
        if (!string.IsNullOrWhiteSpace(query.TransactionType)) url += $"&transactionType={query.TransactionType}";
        var response = await _api.GetAsync(url);
        return response.IsSuccessStatusCode ? await response.Content.ReadAsByteArrayAsync() : null;
    }

    public async Task<byte[]?> DownloadLowStockAsync()
    {
        var response = await _api.GetAsync("api/export/low-stock");
        return response.IsSuccessStatusCode ? await response.Content.ReadAsByteArrayAsync() : null;
    }

    public async Task<byte[]?> DownloadExpiryAlertsAsync(int withinDays = 90)
    {
        var response = await _api.GetAsync($"api/export/expiry-alerts?withinDays={withinDays}");
        return response.IsSuccessStatusCode ? await response.Content.ReadAsByteArrayAsync() : null;
    }
}

public class AuditApiService
{
    private readonly ScopedApiHttpClient _api;

    public AuditApiService(ScopedApiHttpClient api) => _api = api;

    public async Task<List<AuditLogDto>> GetRecentAsync(int count = 100)
    {
        var response = await _api.GetAsync($"api/audit?count={count}");
        var (success, data, _) = await ApiClientHelper.GetApiData<List<AuditLogDto>>(response);
        return success ? data ?? new() : new();
    }
}
