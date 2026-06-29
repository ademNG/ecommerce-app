using System.Net;
using Xunit;

namespace ECommerce.Catalog.Api.Tests;

public class HealthEndpointTests : IClassFixture<CatalogApiFactory>
{
    private readonly HttpClient _client;

    public HealthEndpointTests(CatalogApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetApiHealth_Returns200Ok()
    {
        var response = await _client.GetAsync("/api/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetApiHealth_ReturnsHealthyStatus()
    {
        var response = await _client.GetAsync("/api/health");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Contains("healthy", body);
    }
}
