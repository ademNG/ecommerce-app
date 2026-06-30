namespace ECommerce.Ordering.Api.Models;

public class Order
{
    public int Id { get; set; }
    public required string Customer { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public OrderStatus Status { get; set; } = OrderStatus.Pending;
    public List<OrderItem> Items { get; set; } = [];

    public decimal Total => Items.Sum(i => i.UnitPrice * i.Quantity);
}

public class OrderItem
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public required string ProductName { get; set; }
    public decimal UnitPrice { get; set; }
    public int Quantity { get; set; }
}

public enum OrderStatus
{
    Pending,
    Confirmed,
    Shipped,
    Cancelled
}
