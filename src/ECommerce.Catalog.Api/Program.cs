using ECommerce.Catalog.Api.Data;
using ECommerce.Catalog.Api.Endpoints;
using ECommerce.Catalog.Api.Models;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Aspire: telemetry, health checks, resilience, service discovery.
builder.AddServiceDefaults();

builder.Services.AddOpenApi();
builder.Services.AddDbContext<CatalogDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

var app = builder.Build();

// Aspire default endpoints (/health, /alive).
app.MapDefaultEndpoints();

app.MapGet("/api/health", () => Results.Ok(new { status = "healthy" }))
   .WithTags("Health")
   .ExcludeFromDescription();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapCatalogEndpoints();

// Apply pending migrations and seed initial data if the table is empty.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();
    db.Database.Migrate();

    if (!db.Products.Any())
    {
        db.Products.AddRange(
            new Product { Name = "Mechanical Keyboard", Description = "Hot-swappable RGB keyboard", Price = 119.99m, AvailableStock = 50 },
            new Product { Name = "Wireless Mouse", Description = "Ergonomic 8k DPI mouse", Price = 49.50m, AvailableStock = 120 },
            new Product { Name = "4K Monitor", Description = "27-inch IPS display", Price = 329.00m, AvailableStock = 25 },
            new Product { Name = "USB-C Hub", Description = "7-in-1 docking hub", Price = 39.99m, AvailableStock = 200 });
        db.SaveChanges();
    }
}

app.Run();
