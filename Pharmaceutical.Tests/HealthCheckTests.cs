using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace Pharmaceutical.Tests;

public class HealthCheckTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;

    public HealthCheckTests(TestWebApplicationFactory factory) => _factory = factory;

    [Fact]
    public async Task HealthEndpoint_ReturnsHealthy_InTestingEnvironment()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/health");

        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
    }
}
