# ECommerce.Catalog.Api

**Type:** ASP.NET Core minimal API
**Depends on:** ECommerce.ServiceDefaults, EF Core (in-memory provider)

The Catalog microservice. Owns products. Completely standalone — it knows nothing about
Ordering or the Gateway.

## Files

| File | Responsibility |
|---|---|
| `Models/Product.cs` | The `Product` entity: Id, Name, Description, Price, AvailableStock. |
| `Data/CatalogDbContext.cs` | EF Core context. Configures the schema and **seeds 4 demo products** on startup. |
| `Endpoints/CatalogEndpoints.cs` | REST routes grouped under `/api/products` + the `CreateProductRequest` DTO. |
| `Program.cs` | Composition root: `AddServiceDefaults`, register DbContext + OpenAPI, map endpoints, ensure DB created. |

## Endpoints

| Method | Route | Description |
|---|---|---|
| GET | `/api/products` | List all products |
| GET | `/api/products/{id}` | Get one product (404 if missing) |
| POST | `/api/products` | Create a product |

## Patterns

- **Minimal API** with `MapGroup("/api/products")` and named endpoints.
- **DTO separation** — `CreateProductRequest` is the API contract, kept distinct from the
  `Product` entity.
- `AsNoTracking()` on read queries for efficiency.

## Storage

Uses EF Core's **in-memory** provider (`UseInMemoryDatabase("catalog")`), re-seeded on every
startup. Data does **not** survive a restart. Swap `UseInMemoryDatabase` for `UseNpgsql` /
`UseSqlite` to persist.
