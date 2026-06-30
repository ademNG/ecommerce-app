namespace ECommerce.Ordering.Api.Services;

/// <summary>
/// Typed HTTP client for the Catalog service. The base address ("https+http://catalog")
/// is resolved at runtime by Aspire service discovery — no hard-coded URLs.
/// </summary>
public class CatalogServiceClient(HttpClient httpClient)
{
    public async Task<CatalogProduct?> GetProductAsync(int productId, CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.GetAsync($"/api/products/{productId}", cancellationToken);

        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<CatalogProduct>(cancellationToken);
    }
}

public record CatalogProduct(int Id, string Name, string? Description, decimal Price, int AvailableStock);
