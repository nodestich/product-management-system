using Microsoft.EntityFrameworkCore;

using ProductManagement.Domain.Models;

namespace ProductManagement.Infrastructure.Persistence;

public static class DbSeeder
{
    public static async Task SeedAsync(AppDbContext db, CancellationToken ct = default)
    {
        if (await db.Products.AnyAsync(ct))
        {
            return;
        }

        var products = new[]
        {
            Product.Create("Monitor 32 inches", 249.99m, 12),
            Product.Create("Mechanical keyboard", 189.99m, 8),
            Product.Create("Wireless mouse", 39.99m, 25),
            Product.Create("HDMI cable", 4.75m, 100),
            Product.Create("Mouse pad", 12.00m, 3)
        };

        db.Products.AddRange(products);
        await db.SaveChangesAsync(ct);
    }
}