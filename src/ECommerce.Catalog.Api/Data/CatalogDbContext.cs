using ECommerce.Catalog.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Catalog.Api.Data;

public class CatalogDbContext(DbContextOptions<CatalogDbContext> options) : DbContext(options)
{
    public DbSet<Product> Products => Set<Product>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Product>(entity =>
        {
            entity.HasKey(p => p.Id);
            entity.Property(p => p.Name).HasMaxLength(200).IsRequired();
            entity.Property(p => p.Description).HasMaxLength(2000);
            entity.Property(p => p.Price).HasPrecision(18, 2);
        });

        // Seed data so the service is usable immediately.
        modelBuilder.Entity<Product>().HasData(
            new Product { Id = 1, Name = "Mechanical Keyboard", Description = "Hot-swappable RGB keyboard", Price = 119.99m, AvailableStock = 50 },
            new Product { Id = 2, Name = "Wireless Mouse", Description = "Ergonomic 8k DPI mouse", Price = 49.50m, AvailableStock = 120 },
            new Product { Id = 3, Name = "4K Monitor", Description = "27-inch IPS display", Price = 329.00m, AvailableStock = 25 },
            new Product { Id = 4, Name = "USB-C Hub", Description = "7-in-1 docking hub", Price = 39.99m, AvailableStock = 200 });
    }
}
