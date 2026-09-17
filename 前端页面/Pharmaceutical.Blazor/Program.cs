using Pharmaceutical.Blazor.Components;
using Pharmaceutical.Blazor.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddScoped<AuthStateService>();
builder.Services.AddScoped<ScopedApiHttpClient>();
builder.Services.AddLogging();

var apiBaseUrl = builder.Configuration["ApiSettings:BaseUrl"]
    ?? throw new InvalidOperationException("ApiSettings:BaseUrl is not configured.");

static void ConfigureApiClient(HttpClient client, string baseUrl)
{
    client.BaseAddress = new Uri(baseUrl);
    client.Timeout = TimeSpan.FromSeconds(30);
}

builder.Services.AddHttpClient("ApiRaw", client => ConfigureApiClient(client, apiBaseUrl));
builder.Services.AddHttpClient<AuthApiService>(client => ConfigureApiClient(client, apiBaseUrl));

builder.Services.AddScoped<DrugApiService>();
builder.Services.AddScoped<SupplierApiService>();
builder.Services.AddScoped<StockApiService>();
builder.Services.AddScoped<DashboardApiService>();
builder.Services.AddScoped<PurchaseOrderApiService>();
builder.Services.AddScoped<ExportApiService>();
builder.Services.AddScoped<AuditApiService>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    if (!string.Equals(Environment.GetEnvironmentVariable("DISABLE_HTTPS_REDIRECT"), "true", StringComparison.OrdinalIgnoreCase))
    {
        app.UseHsts();
    }
}

if (app.Environment.IsDevelopment()
    || !string.Equals(Environment.GetEnvironmentVariable("DISABLE_HTTPS_REDIRECT"), "true", StringComparison.OrdinalIgnoreCase))
{
    app.UseHttpsRedirection();
}

app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
