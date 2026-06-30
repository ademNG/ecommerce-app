# ECommerce.Gateway

**Type:** ASP.NET Core + YARP reverse proxy
**Depends on:** ECommerce.ServiceDefaults

The API Gateway — the single entry point in front of the services. Clients (including the
Blazor UI) only ever talk to the gateway; it hides how many services exist and where they run.

## Files

| File | Responsibility |
|---|---|
| `Program.cs` | Loads YARP from config and adds `AddServiceDiscoveryDestinationResolver()`. |
| `appsettings.json` | The **routing table** — YARP routes and clusters. |

## Routing

| Incoming path | Transform | Forwarded to |
|---|---|---|
| `/catalog/{**}` | strip `/catalog` prefix | `catalog` service |
| `/ordering/{**}` | strip `/ordering` prefix | `ordering` service |

Example: `GET /catalog/api/products` → `GET /api/products` on the Catalog service.

## Patterns

- **Edge proxy / single entry point.** Decouples clients from the internal service topology.
- **Config-driven routing.** Routes and clusters live in `appsettings.json`, not code, so they
  can change without recompiling.
- **Service-discovery destinations.** Cluster addresses use `https+http://catalog` /
  `https+http://ordering` — no hard-coded host/port.

## Notes

Requires the `Microsoft.Extensions.ServiceDiscovery.Yarp` package for
`AddServiceDiscoveryDestinationResolver()` (the resolver that connects YARP to Aspire service
discovery).
