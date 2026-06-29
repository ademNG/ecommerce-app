using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace ECommerce.Catalog.Api.Tests;

public class ProductsEndpointTests : IClassFixture<CatalogApiFactory>
{
    private readonly HttpClient _client;

    public ProductsEndpointTests(CatalogApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    // ── GET /api/products ───────────────────────────────────────────────────

    [Fact]
    public async Task GetProducts_Returns200()
    {
        var response = await _client.GetAsync("/api/products");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetProducts_ReturnsSeededList()
    {
        var products = await _client.GetFromJsonAsync<List<ProductDto>>("/api/products");

        Assert.NotNull(products);
        // Au moins les 4 produits seedés (d'autres tests POST peuvent en ajouter)
        Assert.True(products.Count >= 4);
    }

    [Fact]
    public async Task GetProducts_ContainsExpectedSeedItems()
    {
        var products = await _client.GetFromJsonAsync<List<ProductDto>>("/api/products");

        Assert.NotNull(products);
        Assert.Contains(products, p => p.Name == "Mechanical Keyboard" && p.Price == 119.99m);
        Assert.Contains(products, p => p.Name == "Wireless Mouse");
        Assert.Contains(products, p => p.Name == "4K Monitor");
        Assert.Contains(products, p => p.Name == "USB-C Hub");
    }

    // ── GET /api/products/{id} ──────────────────────────────────────────────

    [Fact]
    public async Task GetProductById_ExistingId_Returns200WithProduct()
    {
        var product = await _client.GetFromJsonAsync<ProductDto>("/api/products/1");

        Assert.NotNull(product);
        Assert.Equal(1, product.Id);
        Assert.Equal("Mechanical Keyboard", product.Name);
        Assert.Equal(119.99m, product.Price);
        Assert.Equal(50, product.AvailableStock);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    public async Task GetProductById_AllSeedIds_Return200(int id)
    {
        var response = await _client.GetAsync($"/api/products/{id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetProductById_NonExistingId_Returns404()
    {
        var response = await _client.GetAsync("/api/products/999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(99999)]
    public async Task GetProductById_InvalidOrMissingId_Returns404(int id)
    {
        var response = await _client.GetAsync($"/api/products/{id}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // ── POST /api/products ──────────────────────────────────────────────────

    [Fact]
    public async Task CreateProduct_ValidRequest_Returns201()
    {
        var request = new { Name = "New Headset", Description = "Noise-cancelling", Price = 79.99m, AvailableStock = 30 };

        var response = await _client.PostAsJsonAsync("/api/products", request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task CreateProduct_ValidRequest_ReturnsCreatedProduct()
    {
        var request = new { Name = "Webcam HD", Description = "1080p webcam", Price = 59.99m, AvailableStock = 15 };

        var response = await _client.PostAsJsonAsync("/api/products", request);
        var created = await response.Content.ReadFromJsonAsync<ProductDto>();

        Assert.NotNull(created);
        Assert.True(created.Id > 0);
        Assert.Equal("Webcam HD", created.Name);
        Assert.Equal(59.99m, created.Price);
    }

    [Fact]
    public async Task CreateProduct_ValidRequest_IsRetrievableAfterCreation()
    {
        var request = new { Name = "Desk Lamp", Description = "LED desk lamp", Price = 29.99m, AvailableStock = 80 };

        var createResponse = await _client.PostAsJsonAsync("/api/products", request);
        var created = await createResponse.Content.ReadFromJsonAsync<ProductDto>();

        Assert.NotNull(created);

        var getResponse = await _client.GetAsync($"/api/products/{created.Id}");

        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
    }

    // ── DTO local ───────────────────────────────────────────────────────────

    private record ProductDto(int Id, string Name, string? Description, decimal Price, int AvailableStock);
}
