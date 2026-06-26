using ECommerce.Ordering.Api.Data;
using ECommerce.Ordering.Api.Endpoints;
using ECommerce.Ordering.Api.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Aspire: telemetry, health checks, resilience, service discovery.
builder.AddServiceDefaults();

builder.Services.AddOpenApi();
builder.Services.AddDbContext<OrderingDbContext>(options =>
    options.UseInMemoryDatabase("ordering"));

// Serialize enums (e.g. OrderStatus) as strings rather than integers.
builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter()));

// Typed client to the Catalog service. "catalog" is the Aspire resource name;
// service discovery + standard resilience handlers come from AddServiceDefaults.
builder.Services.AddHttpClient<CatalogServiceClient>(client =>
    client.BaseAddress = new Uri("https+http://catalog"));

var app = builder.Build();

// Aspire default endpoints (/health, /alive).
app.MapDefaultEndpoints();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapOrderingEndpoints();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<OrderingDbContext>();
    db.Database.EnsureCreated();
}

app.Run();
