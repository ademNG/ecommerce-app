# ECommerce.ServiceDefaults

**Type:** class library
**Referenced by:** every service (Catalog, Ordering, Gateway, Web) — *not* the AppHost

The shared foundation. Holds the cross-cutting concerns every microservice needs, written
once so each service stays thin and consistent.

## Files

| File | Responsibility |
|---|---|
| `Extensions.cs` | Extension methods that wire up observability, health, resilience, and service discovery. |

## What `AddServiceDefaults()` gives you

Each service calls `builder.AddServiceDefaults();` in one line and inherits:

| Concern | Detail |
|---|---|
| `ConfigureOpenTelemetry()` | Distributed **tracing + metrics + logging**, exported over OTLP to the Aspire dashboard. |
| `AddDefaultHealthChecks()` | `/health` (ready to serve traffic?) and `/alive` (process is up?). |
| **Service discovery** | Lets logical names like `https+http://catalog` resolve to real addresses at runtime. |
| **Standard resilience** | Auto-retry, timeout, and circuit-breaker applied to **every** `HttpClient`. |

## Pattern

DRY infrastructure / composition root helper. Centralizing this means observability and
resilience are identical across services and can't drift.

## Notes

`MapDefaultEndpoints()` (also defined here) maps the `/health` and `/alive` endpoints and is
called from each service's `Program.cs`.
