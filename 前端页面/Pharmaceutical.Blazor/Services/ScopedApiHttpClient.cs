using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace Pharmaceutical.Blazor.Services;

/// <summary>
/// Circuit-scoped API client that attaches JWT from the same AuthStateService instance as Blazor components.
/// Avoids DelegatingHandler scope isolation in Blazor Server IHttpClientFactory.
/// </summary>
public class ScopedApiHttpClient
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly AuthStateService _authState;

    public ScopedApiHttpClient(IHttpClientFactory httpClientFactory, AuthStateService authState)
    {
        _httpClientFactory = httpClientFactory;
        _authState = authState;
    }

    public Task<HttpResponseMessage> GetAsync(string url, CancellationToken cancellationToken = default)
        => SendAsync(new HttpRequestMessage(HttpMethod.Get, url), cancellationToken);

    public Task<HttpResponseMessage> PostAsJsonAsync<T>(string url, T value, CancellationToken cancellationToken = default)
        => SendAsync(HttpRequestMessageWithJson(HttpMethod.Post, url, value), cancellationToken);

    public Task<HttpResponseMessage> PutAsJsonAsync<T>(string url, T value, CancellationToken cancellationToken = default)
        => SendAsync(HttpRequestMessageWithJson(HttpMethod.Put, url, value), cancellationToken);

    public Task<HttpResponseMessage> DeleteAsync(string url, CancellationToken cancellationToken = default)
        => SendAsync(new HttpRequestMessage(HttpMethod.Delete, url), cancellationToken);

    public Task<HttpResponseMessage> PostAsync(string url, HttpContent? content, CancellationToken cancellationToken = default)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, url) { Content = content };
        return SendAsync(request, cancellationToken);
    }

    public async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken = default)
    {
        await _authState.WhenInitialized;

        var hadToken = !string.IsNullOrEmpty(_authState.Token);
        if (hadToken)
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _authState.Token);

        var client = _httpClientFactory.CreateClient("ApiRaw");
        var response = await client.SendAsync(request, cancellationToken);

        if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized && hadToken)
            _ = _authState.NotifyUnauthorizedAsync();

        return response;
    }

    private static HttpRequestMessage HttpRequestMessageWithJson<T>(HttpMethod method, string url, T value)
    {
        var request = new HttpRequestMessage(method, url)
        {
            Content = JsonContent.Create(value)
        };
        return request;
    }
}
