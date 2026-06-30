# ECommerce.Ordering.Api

**Type:** ASP.NET Core minimal API
**Depends on:** ECommerce.ServiceDefaults, EF Core (in-memory provider), **calls Catalog over HTTP**

The Ordering microservice. Owns orders, and validates products against the Catalog service
before accepting an order.

## Files

| File | Responsibility |
|---|---|
| `Models/Order.cs` | `Order` + `OrderItem` entities and the `OrderStatus` enum. `Total` is computed from items. |
| `Data/OrderingDbContext.cs` | EF Core context. Order→Items relationship with cascade delete. |
| `Services/CatalogServiceClient.cs` | **Typed HTTP client** to Catalog (`https+http://catalog`) + the `CatalogProduct` DTO. |
| `Endpoints/OrderingEndpoints.cs` | REST routes grouped under `/api/orders` + request DTOs. |
| `Program.cs` | Registers DbContext, the typed client, and enum-as-string JSON serialization. |

## Endpoints

| Method | Route | Description |
|---|---|---|
| GET | `/api/orders` | List orders (with items) |
| GET | `/api/orders/{id}` | Get one order (404 if missing) |
| POST | `/api/orders` | Create an order — validates every product via Catalog |

## The key inter-service pattern

On `POST /api/orders`, the handler loops over the requested items and calls
`catalog.GetProductAsync(id)` for each. It:

1. **Rejects** the order (400) if any product doesn't exist.
2. **Snapshots** the product's current name and price into the `OrderItem`, so the order is a
   point-in-time record rather than a live reference.

This is the synchronous, HTTP-based communication between services. Because the client is
registered via `AddHttpClient<CatalogServiceClient>` it inherits service discovery and the
standard resilience handlers from ServiceDefaults (auto-retry, timeout, circuit-breaker).

## Storage

In-memory (`UseInMemoryDatabase("ordering")`). Orders do **not** survive a restart.
