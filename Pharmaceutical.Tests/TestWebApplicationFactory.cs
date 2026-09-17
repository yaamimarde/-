using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Pharmaceutical.Core.DTOs;
using Pharmaceutical.Core.Interfaces;
using Moq;

namespace Pharmaceutical.Tests;

public class TestWebApplicationFactory : WebApplicationFactory<Program>
{
    public TestWebApplicationFactory()
    {
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Testing");
        Environment.SetEnvironmentVariable("JwtSettings__Secret", "test-secret-key-at-least-32-characters-long-for-jwt");
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["JwtSettings:Secret"] = "test-secret-key-at-least-32-characters-long-for-jwt",
                ["JwtSettings:Issuer"] = "Pharmaceutical.WebAPI",
                ["JwtSettings:Audience"] = "Pharmaceutical.Blazor",
                ["ConnectionStrings:DefaultConnection"] = "server=127.0.0.1;user=root;database=pharmaceutical_test;port=3306;password=test_password;",
            });
        });
        builder.ConfigureServices(services =>
        {
            var drugServiceDescriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(IDrugService));
            if (drugServiceDescriptor != null)
                services.Remove(drugServiceDescriptor);

            var mockDrugService = new Mock<IDrugService>();
            mockDrugService.Setup(s => s.GetPagedAsync(null, 1, 20, true))
                .ReturnsAsync(new PagedResult<DrugDto>());
            services.AddSingleton(mockDrugService.Object);
        });
    }
}
