using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.JSInterop;
using Pharmaceutical.Core.DTOs;

namespace Pharmaceutical.Blazor.Services;

public class AuthStateService
{
    private readonly IJSRuntime _jsRuntime;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<AuthStateService> _logger;
    private readonly TaskCompletionSource _initTcs = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private string? _token;
    private string? _username;
    private string? _displayName;
    private List<string> _roles = new();
    private bool _initialized;
    private bool _sessionInvalidated;

    public event Func<Task>? Unauthorized;
    public event Action? AuthChanged;

    public Task WhenInitialized => _initTcs.Task;
    public bool IsInitialized => _initTcs.Task.IsCompleted;
    public string? Token => _token;
    public string? Username => _username;
    public string? DisplayName => _displayName;
    public IReadOnlyList<string> Roles => _roles;
    public bool IsAuthenticated => !string.IsNullOrEmpty(_token);
    public bool SessionInvalidated => _sessionInvalidated;
    public bool IsAdmin => _roles.Contains("Admin");
    public bool CanOperate => IsAdmin || _roles.Contains("Operator");

    public AuthStateService(
        IJSRuntime jsRuntime,
        IHttpClientFactory httpClientFactory,
        ILogger<AuthStateService> logger)
    {
        _jsRuntime = jsRuntime;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    private static readonly JsonSerializerOptions AuthJsonOptions = new() { PropertyNameCaseInsensitive = true };

    public async Task InitializeAsync()
    {
        if (_initialized) return;
        _initialized = true;
        try
        {
            var jsTask = _jsRuntime.InvokeAsync<string?>("localStorage.getItem", "pharm_auth").AsTask();
            var completed = await Task.WhenAny(jsTask, Task.Delay(TimeSpan.FromSeconds(3)));
            if (completed != jsTask)
            {
                _logger.LogWarning("Auth state restore timed out waiting for browser JS");
                return;
            }

            var json = await jsTask;
            if (json != null)
            {
                var data = JsonSerializer.Deserialize<AuthData>(json, AuthJsonOptions);
                if (data != null)
                {
                    _token = data.Token;
                    _username = data.Username;
                    _displayName = data.DisplayName;
                    _roles = data.Roles ?? new();
                }
            }

            if (!string.IsNullOrEmpty(_token))
                await ValidateRestoredTokenAsync();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to restore auth state");
        }
        finally
        {
            _initTcs.TrySetResult();
            NotifyAuthChanged();
        }
    }

    private async Task ValidateRestoredTokenAsync()
    {
        try
        {
            var client = _httpClientFactory.CreateClient("ApiRaw");
            var request = new HttpRequestMessage(HttpMethod.Get, "api/auth/me");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _token);
            var response = await client.SendAsync(request);
            var (success, data, _) = await ApiClientHelper.GetApiData<UserDto>(response);
            if (!success || data == null)
            {
                _logger.LogWarning("Restored token rejected by API, clearing auth state");
                await ClearLocalAuthAsync();
                return;
            }

            _username = data.Username;
            _displayName = data.DisplayName;
            _roles = new List<string> { data.Role };
            await PersistAsync();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Token validation failed, clearing auth state");
            await ClearLocalAuthAsync();
        }
    }

    public async Task SetAuthAsync(LoginResponseDto response)
    {
        _sessionInvalidated = false;
        _token = response.Token;
        _username = response.Username;
        _displayName = response.DisplayName;
        _roles = response.Roles;
        await PersistAsync();
        NotifyAuthChanged();
    }

    public async Task ClearAsync()
    {
        await ClearLocalAuthAsync();
        NotifyAuthChanged();
    }

    private async Task ClearLocalAuthAsync()
    {
        _token = null;
        _username = null;
        _displayName = null;
        _roles = new();
        try { await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", "pharm_auth"); }
        catch { /* ignore */ }
    }

    private void NotifyAuthChanged() => AuthChanged?.Invoke();

    public async Task NotifyUnauthorizedAsync()
    {
        if (_sessionInvalidated) return;
        _sessionInvalidated = true;
        await ClearAsync();
        if (Unauthorized != null) await Unauthorized.Invoke();
    }

    public static bool IsAuthErrorMessage(string? message) =>
        !string.IsNullOrEmpty(message)
        && (message.Contains("登录已过期", StringComparison.Ordinal)
            || message.Contains("请重新登录", StringComparison.Ordinal)
            || message.Contains("用户名或密码错误", StringComparison.Ordinal));

    private async Task PersistAsync()
    {
        try
        {
            var data = new AuthData { Token = _token, Username = _username, DisplayName = _displayName, Roles = _roles };
            await _jsRuntime.InvokeVoidAsync("localStorage.setItem", "pharm_auth", JsonSerializer.Serialize(data, AuthJsonOptions));
        }
        catch { /* ignore */ }
    }

    private class AuthData
    {
        public string? Token { get; set; }
        public string? Username { get; set; }
        public string? DisplayName { get; set; }
        public List<string>? Roles { get; set; }
    }
}

public static class ApiClientHelper
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public static async Task<(bool Success, T? Data, string Message)> GetApiData<T>(HttpResponseMessage response)
    {
        var content = await response.Content.ReadAsStringAsync();

        if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
        {
            var message = TryParseApiMessage(content) ?? "登录已过期，请重新登录";
            return (false, default, message);
        }

        if (response.StatusCode == System.Net.HttpStatusCode.BadRequest
            && content.Contains("\"errors\"", StringComparison.OrdinalIgnoreCase))
        {
            var validationMessage = TryParseValidationErrors(content);
            if (!string.IsNullOrEmpty(validationMessage))
                return (false, default, validationMessage);
        }

        try
        {
            var apiResponse = JsonSerializer.Deserialize<ApiResponse<T>>(content, JsonOptions);
            if (apiResponse == null) return (false, default, "无法解析服务器响应");
            return (apiResponse.Success, apiResponse.Data, apiResponse.Message ?? string.Empty);
        }
        catch
        {
            return (false, default, "无法解析服务器响应");
        }
    }

    private static string? TryParseApiMessage(string content)
    {
        try
        {
            using var doc = JsonDocument.Parse(content);
            if (doc.RootElement.TryGetProperty("message", out var msg) && msg.GetString() is { Length: > 0 } m)
                return m;
        }
        catch { /* ignore */ }
        return null;
    }

    private static string? TryParseValidationErrors(string content)
    {
        try
        {
            using var doc = JsonDocument.Parse(content);
            if (!doc.RootElement.TryGetProperty("errors", out var errors)) return null;
            var messages = new List<string>();
            foreach (var property in errors.EnumerateObject())
                foreach (var error in property.Value.EnumerateArray())
                    if (error.GetString() is { } m) messages.Add(m);
            return messages.Count > 0 ? string.Join("; ", messages) : null;
        }
        catch { return null; }
    }
}
