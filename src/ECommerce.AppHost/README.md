# ECommerce.AppHost

**Type:** .NET Aspire AppHost (executable — the project you run)
**References:** Catalog.Api, Ordering.Api, Gateway, Web

The orchestrator. Running this one project launches **all** services plus the Aspire
dashboard. It defines the composition graph of the whole system.

## Files

| File | Responsibility |
|---|---|
| `AppHost.cs` | Declares every service, their references, and startup order. |

## The composition graph

```csharp
var catalog  = builder.AddProject<Projects.ECommerce_Catalog_Api>("catalog");

var ordering = builder.AddProject<Projects.ECommerce_Ordering_Api>("ordering")
                      .WithReference(catalog)
                      .WaitFor(catalog);

var gateway  = builder.AddProject<Projects.ECommerce_Gateway>("gateway")
                      .WithReference(catalog)
                      .WithReference(ordering)
                      .WaitFor(catalog)
                      .WaitFor(ordering);

builder.AddProject<Projects.ECommerce_Web>("web")
       .WithReference(gateway)
       .WaitFor(gateway)
       .WithExternalHttpEndpoints();
```

## How the pieces connect

| API | Meaning |
|---|---|
| `AddProject<...>("name")` | Registers a service. The **string name** is exactly the hostname that service discovery resolves — that's why `https+http://catalog` works. |
| `.WithReference(x)` | Injects `x`'s address into the dependent service's configuration. |
| `.WaitFor(x)` | Enforces startup order (don't start until `x` is healthy). |
| `.WithExternalHttpEndpoints()` | Exposes the resource (the Web UI) outside the app network. |

## Dependency flow

```
Web → Gateway → { Catalog, Ordering }     and     Ordering → Catalog
                  (all services use ServiceDefaults)
AppHost orchestrates everything — it knows all of them; none of them know it.
```

## Run

```bash
dotnet run --project src/ECommerce.AppHost
```

Then open the dashboard URL printed in the console (it contains a login token).
