using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Products.Domain.Entities;

namespace Products.Infrastructure.Persistence;

public static class DbSeeder
{
    public static async Task SeedAsync(ProductsDbContext dbContext, ILogger logger, CancellationToken cancellationToken = default)
    {
        if (await dbContext.Products.AnyAsync(cancellationToken))
        {
            return;
        }

        var utcNow = DateTime.UtcNow;

        dbContext.Products.AddRange(
            new Product { Name = "Wireless Mouse", Description = "Ergonomic 2.4GHz wireless mouse", Price = 24.99m, Stock = 150, CreatedAtUtc = utcNow },
            new Product { Name = "Mechanical Keyboard", Description = "Tenkeyless mechanical keyboard, brown switches", Price = 89.90m, Stock = 75, CreatedAtUtc = utcNow },
            new Product { Name = "USB-C Hub", Description = "7-in-1 USB-C hub with HDMI and PD", Price = 45.50m, Stock = 200, CreatedAtUtc = utcNow },
            new Product { Name = "27\" Monitor", Description = "27-inch QHD IPS monitor, 144Hz", Price = 299.00m, Stock = 30, CreatedAtUtc = utcNow },
            new Product { Name = "Laptop Stand", Description = "Adjustable aluminium laptop stand", Price = 32.00m, Stock = 0, CreatedAtUtc = utcNow },
            new Product { Name = "Webcam 1080p", Description = "Full HD webcam with privacy shutter", Price = 59.99m, Stock = 12, CreatedAtUtc = utcNow });

        await dbContext.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Database seeded with initial products.");
    }
}
