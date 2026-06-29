# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Commands

```bash
# Run the full app (Aspire orchestrator — opens dashboard with Login URL printed in terminal)
dotnet run --project src/ECommerce.AppHost

# First-time SSL setup (prevents UntrustedRoot errors)
dotnet dev-certs https --trust

# Build / restore
dotnet restore ECommerce.slnx
dotnet build ECommerce.slnx --configuration Release

# Run all tests
dotnet test ECommerce.slnx

# Run a single test class
dotnet test tests/ECommerce.Catalog.Api.Tests --filter "FullyQualifiedName~HealthEndpointTests"

# Docker — build context is always the repo root
docker build -t ecommerce-catalog:latest -f src/ECommerce.Catalog.Api/Dockerfile .
docker compose up --build          # full local stack
docker compose down                # stop and remove containers
```

## Architecture

```
Web (Blazor) ──► Gateway (YARP) ──► Catalog.Api
                                └──► Ordering.Api ──► Catalog.Api
```

All services start from `src/ECommerce.AppHost/AppHost.cs`, which declares the dependency graph. The service names defined there (`"catalog"`, `"ordering"`, `"gateway"`) become the logical addresses used everywhere.

**Service discovery** — services never use hardcoded URLs. They reference each other as `https+http://catalog` (Aspire scheme). Aspire resolves these at runtime from `AppHost.cs`. In Docker Compose this mechanism is replaced by environment variables: `services__catalog__http__0=http://catalog:8080`.

**Gateway routing** — YARP routes are defined in `src/ECommerce.Gateway/appsettings.json`. `/catalog/{**}` strips the prefix and forwards to the catalog cluster; `/ordering/{**}` to ordering. The `Web` service calls the gateway for everything; it never calls API services directly.

**ServiceDefaults** — `src/ECommerce.ServiceDefaults/Extensions.cs` is a shared library called by every service. `AddServiceDefaults()` wires OpenTelemetry, resilience (Polly), and service discovery. `MapDefaultEndpoints()` exposes `/health` (readiness) and `/alive` (liveness) on every service unconditionally (required for Kubernetes probes).

**Health endpoints** — every service has `/health` and `/alive` via `MapDefaultEndpoints()`. `Catalog.Api` additionally exposes `GET /api/health → 200 { status: "healthy" }` as an explicit minimal API endpoint.

**Databases** — both `Catalog.Api` and `Ordering.Api` use EF Core InMemory. Seed data is declared in `OnModelCreating` via `HasData` and applied by `EnsureCreated()` at startup. Data does not persist across restarts.

**Inter-service call** — `Ordering.Api` calls `Catalog.Api` to validate products at order creation time. The typed client `CatalogServiceClient` uses base address `https+http://catalog` wired in `Ordering.Api/Program.cs`.

## Tests

Integration tests live in `tests/ECommerce.Catalog.Api.Tests/` and use `WebApplicationFactory<Program>`. For `Program` to be accessible from the test project, `src/ECommerce.Catalog.Api/ECommerce.Catalog.Api.csproj` declares `<InternalsVisibleTo Include="ECommerce.Catalog.Api.Tests" />`. Tests use the InMemory database seeded on startup — no Docker or external dependencies needed.

## Docker

All Dockerfiles use a two-stage build: `mcr.microsoft.com/dotnet/sdk:10.0` to compile, `mcr.microsoft.com/dotnet/aspnet:10.0-noble-chiseled` as the runtime (minimal, no shell, non-root via `USER $APP_UID`, port 8080). The build context must always be the **repo root** — `COPY` instructions start with `src/`. Do not use `--no-restore` with `dotnet publish` inside Docker; the NuGet cache from a prior `RUN dotnet restore` layer is not visible to subsequent steps.
