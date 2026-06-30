var builder = WebApplication.CreateBuilder(args);

// Aspire: telemetry, health checks, resilience, service discovery.
builder.AddServiceDefaults();

// YARP reverse proxy. Routes/clusters are loaded from configuration
// (appsettings.json) and destinations are resolved via Aspire service discovery.
builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"))
    .AddServiceDiscoveryDestinationResolver();

var app = builder.Build();

app.MapDefaultEndpoints();

app.MapReverseProxy();

app.Run();
