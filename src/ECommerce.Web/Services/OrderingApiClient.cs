namespace ECommerce.Web.Services;

/// <summary>
/// Talks to the Ordering service through the gateway ("/ordering/...").
/// Base address is resolved by Aspire service discovery.
/// </summary>
public class OrderingApiClient(HttpClient httpClient)
{
    public async Task<IReadOnlyList<OrderDto>> GetOrdersAsync(CancellationToken ct = default) =>
        await httpClient.GetFromJsonAsync<List<OrderDto>>("/ordering/api/orders", ct) ?? [];

    public async Task<(bool Success, string? Error)> CreateOrderAsync(CreateOrderDto order, CancellationToken ct = default)
    {
        using var response = await httpClient.PostAsJsonAsync("/ordering/api/orders", order, ct);
        if (response.IsSuccessStatusCode)
        {
            return (true, null);
        }

        var detail = await response.Content.ReadAsStringAsync(ct);
        return (false, string.IsNullOrWhiteSpace(detail) ? response.ReasonPhrase : detail);
    }
}

public record OrderDto(int Id, string Customer, DateTimeOffset CreatedAt, string Status, decimal Total, List<OrderItemDto> Items);
public record OrderItemDto(int ProductId, string ProductName, decimal UnitPrice, int Quantity);
public record CreateOrderDto(string Customer, List<OrderLineDto> Items);
public record OrderLineDto(int ProductId, int Quantity);
