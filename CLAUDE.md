# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## What this is

A .NET 10 / ASP.NET Core e-commerce app split into microservices and orchestrated by **.NET Aspire**. Six projects (see `ECommerce.slnx`): an Aspire AppHost, a Blazor web frontend, a YARP gateway, two REST services (Catalog, Ordering), and a shared ServiceDefaults library. Databases are **EF Core in-memory** — no DB to install, data resets on every restart. Docker is not used.

## Commands

```bash
# On this machine the .NET 10 SDK is not on PATH — run this first in every new terminal:
export PATH="/usr/local/share/dotnet:$PATH"

# One-time: trust the dev HTTPS cert (without it the Aspire dashboard fails with UntrustedRoot)
dotnet dev-certs https --trust

# Run the whole app — ALWAYS launch via the AppHost, never a single service.
# It starts all services + the Aspire dashboard. Open the "Login URL" it prints (has ?t=<token>).
dotnet run --project src/ECommerce.AppHost

# Build / restore the full solution
dotnet build ECommerce.slnx
```

The dashboard port changes every launch — always use the URL printed in your terminal. Stop everything with `Ctrl+C` in the AppHost terminal.

**There is no test project.** Verify changes by running the AppHost and exercising the `web` frontend or the gateway routes (see API table in `README.md`). Each service also exposes `/openapi/v1.json` in Development.

## Architecture

Request flow: **Web (Blazor) → Gateway (YARP) → Catalog / Ordering**. The Ordering service additionally calls Catalog directly to validate products at order time.

```
Web ──HTTP──> Gateway ──/catalog──> Catalog
                      ──/ordering─> Ordering ──validates each product──> Catalog
```

`AppHost.cs` is the source of truth for the topology: it declares each service, the `.WithReference(...)` dependencies, and `.WaitFor(...)` startup ordering. Edit it when adding a service or changing wiring.

### Service discovery — the key convention

No service ever hard-codes another's address. HttpClients use logical names resolved at runtime by Aspire service discovery:

- Ordering → Catalog: `new Uri("https+http://catalog")` (`CatalogServiceClient`)
- Web → Gateway: `new Uri("https+http://gateway")` (`CatalogApiClient`, `OrderingApiClient`)
- Gateway → services: addresses `https+http://catalog` / `https+http://ordering` in `appsettings.json`, resolved via `.AddServiceDiscoveryDestinationResolver()`

The name (`catalog`, `ordering`, `gateway`) must match the resource name given in `AppHost.cs`. The `https+http://` scheme means "prefer HTTPS, fall back to HTTP."

### Gateway routing

`ECommerce.Gateway/appsettings.json` defines the YARP routes/clusters. `/catalog/{**catch-all}` and `/ordering/{**catch-all}` strip their prefix (`PathRemovePrefix`) before forwarding. So a frontend call to `/catalog/api/products` reaches the Catalog service as `/api/products`.

### ServiceDefaults

`ECommerce.ServiceDefaults/Extensions.cs` provides `AddServiceDefaults()` (called first in every service's `Program.cs`) and `MapDefaultEndpoints()`. It wires OpenTelemetry, health checks (`/health`, `/alive` — Development only), standard HTTP resilience handlers, and service discovery for all HttpClients. Cross-cutting concerns belong here, not in individual services.

## Conventions when editing services

- **Minimal APIs grouped in extension methods.** Each service has `Endpoints/<Name>Endpoints.cs` with a `MapXEndpoints(this IEndpointRouteBuilder)` extension, called from `Program.cs`. Add routes there, not inline in `Program.cs`.
- **Records for DTOs, co-located with their use.** Request/response DTOs are `record` types declared at the bottom of the endpoint or client file (e.g. `CreateProductRequest`, `OrderDto`). The Web and Ordering layers define their own DTO copies rather than sharing a model project — keep them in sync by hand.
- **EF Core in-memory, seeded on startup.** Each service has a `DbContext` using `.UseInMemoryDatabase(...)`. Seed data lives in `OnModelCreating` via `HasData(...)` (see `CatalogDbContext`); `Program.cs` calls `db.Database.EnsureCreated()` at boot. Persisting data would mean swapping the in-memory provider for a real one.
- **Cross-service validation is synchronous HTTP.** `POST /api/orders` loops over items and calls `CatalogServiceClient.GetProductAsync` per product; a missing product returns `400`. This is the canonical example of a service-to-service call.
- Ordering serializes enums (e.g. `OrderStatus`) as strings via `JsonStringEnumConverter` configured in its `Program.cs`.

## Notes

- The Web frontend is Blazor Server (interactive server components); pages are in `ECommerce.Web/Components/Pages/`.
- `bin/` and `obj/` are build output and are committed in this repo's current state but should not be edited.
