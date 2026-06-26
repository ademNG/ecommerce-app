namespace ECommerce.Web.Services;

/// <summary>
/// Talks to the Catalog service through the gateway ("/catalog/...").
/// Base address is resolved by Aspire service discovery.
/// </summary>
public class CatalogApiClient(HttpClient httpClient)
{
    public async Task<IReadOnlyList<ProductDto>> GetProductsAsync(CancellationToken ct = default) =>
        await httpClient.GetFromJsonAsync<List<ProductDto>>("/catalog/api/products", ct) ?? [];
}

public record ProductDto(int Id, string Name, string? Description, decimal Price, int AvailableStock);
