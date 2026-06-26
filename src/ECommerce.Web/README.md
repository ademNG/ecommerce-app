# ECommerce.Web

**Type:** Blazor Web App (interactive server render mode)
**Depends on:** ECommerce.ServiceDefaults, **calls the Gateway**

The web frontend. It is just another client of the system — it goes through the Gateway
exactly like an external caller, never directly to Catalog or Ordering.

## Files

| File | Responsibility |
|---|---|
| `Services/CatalogApiClient.cs` | Typed client → `/catalog/...` via the gateway. `ProductDto`. |
| `Services/OrderingApiClient.cs` | Typed client → `/ordering/...` via the gateway. Order DTOs. |
| `Components/Pages/Home.razor` | Landing page with links to Catalog and Orders. |
| `Components/Pages/Products.razor` | Catalog list + in-memory cart + place-order form. |
| `Components/Pages/Orders.razor` | Order history with a refresh button. |
| `Components/Layout/NavMenu.razor` | Navigation: Catalog / Orders. |
| `Program.cs` | Registers the two typed clients pointing at `https+http://gateway`. |

## Pages / flow

1. **Catalog** (`/products`) — lists products from Catalog, lets you add items to a cart,
   enter a customer name, and place an order (POST to Ordering).
2. **Orders** (`/orders`) — lists orders from Ordering.

## Patterns

- **Typed `HttpClient`s** registered with `AddHttpClient<T>` — they inherit service discovery
  and resilience from ServiceDefaults.
- **UI as a gateway client** — both clients target `https+http://gateway`, so the UI is
  decoupled from the internal service topology, same as any other consumer.

## Notes

The default Blazor template's `Counter` and `Weather` demo pages were removed.
