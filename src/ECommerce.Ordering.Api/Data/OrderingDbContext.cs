using ECommerce.Ordering.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Ordering.Api.Data;

public class OrderingDbContext(DbContextOptions<OrderingDbContext> options) : DbContext(options)
{
    public DbSet<Order> Orders => Set<Order>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Order>(entity =>
        {
            entity.HasKey(o => o.Id);
            entity.Property(o => o.Customer).HasMaxLength(200).IsRequired();
            entity.Ignore(o => o.Total);
            entity.HasMany(o => o.Items).WithOne().OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<OrderItem>(entity =>
        {
            entity.HasKey(i => i.Id);
            entity.Property(i => i.ProductName).HasMaxLength(200).IsRequired();
            entity.Property(i => i.UnitPrice).HasPrecision(18, 2);
        });
    }
}
