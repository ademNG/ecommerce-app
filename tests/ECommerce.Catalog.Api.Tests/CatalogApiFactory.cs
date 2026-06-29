using ECommerce.Catalog.Api.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ECommerce.Catalog.Api.Tests;

public class CatalogApiFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<CatalogDbContext>));
            if (descriptor != null)
                services.Remove(descriptor);

            services.AddDbContext<CatalogDbContext>(options =>
                options.UseInMemoryDatabase("TestCatalog_" + Guid.NewGuid()));
        });
    }
}
