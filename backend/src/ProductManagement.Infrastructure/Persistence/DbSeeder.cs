using Microsoft.EntityFrameworkCore;

using ProductManagement.Domain.Models;

namespace ProductManagement.Infrastructure.Persistence;

public static class DbSeeder
{
    // Fixed seed so a recreated database always gets the same catalogue and predictable IDs.
    private const int Seed = 20260101;
    private const int MaxSeedStock = 120;

    private static readonly (string Name, decimal Price)[] Catalogue =
    [
        ("Monitor 32 inches 4K", 349.99m),
        ("Monitor 27 inches 144Hz", 249.99m),
        ("Ultrawide monitor 34 inches", 499.00m),
        ("Mechanical keyboard brown switches", 189.99m),
        ("Mechanical keyboard blue switches", 149.99m),
        ("Low-profile wireless keyboard", 99.90m),
        ("Wireless mouse", 39.99m),
        ("Gaming mouse 26000 DPI", 69.99m),
        ("Vertical ergonomic mouse", 45.50m),
        ("HDMI 2.1 cable 2m", 14.75m),
        ("DisplayPort cable 2m", 16.99m),
        ("USB-C to USB-C cable 1m", 12.99m),
        ("USB-C hub 7-in-1", 59.99m),
        ("Thunderbolt 4 dock", 289.00m),
        ("Mouse pad XXL", 22.00m),
        ("Desk mat with wrist rest", 34.50m),
        ("Laptop stand aluminium", 49.99m),
        ("Monitor arm single", 79.00m),
        ("Dual monitor arm", 129.00m),
        ("1TB NVMe SSD", 89.99m),
        ("2TB NVMe SSD", 159.99m),
        ("4TB external hard drive", 109.99m),
        ("16 GB DDR5 RAM stick", 79.99m),
        ("32 GB DDR5 RAM kit", 149.99m),
        ("8TB NAS drive", 219.00m),
        ("Wi-Fi 6E PCIe card", 44.99m),
        ("Gigabit network switch 8-port", 34.99m),
        ("Cat 6 Ethernet cable 5m", 9.49m),
        ("Mesh router 2-pack", 199.00m),
        ("Bluetooth 5.3 adapter", 19.99m),
        ("Webcam 1080p 60fps", 79.99m),
        ("Streaming microphone USB", 129.00m),
        ("Headset with noise cancelling mic", 159.00m),
        ("Studio headphones", 199.00m),
        ("Desktop speakers 2.0", 99.00m),
        ("Laptop cooling pad", 29.99m),
        ("140mm PC case fan", 24.99m),
        ("RGB fan kit 3-pack", 64.99m),
        ("650W 80+ Gold PSU", 99.99m),
        ("Mid-tower PC case", 89.99m),
        ("CPU air cooler", 45.00m),
        ("Thermal paste tube", 8.99m),
        ("Cable management kit", 17.50m),
        ("Laptop bag 15.6 inches", 59.00m),
        ("Surge protector 8 outlets", 27.99m)
    ];

    public static async Task SeedAsync(AppDbContext db, CancellationToken ct = default)
    {
        if (await db.Products.AnyAsync(ct))
        {
            return;
        }

        var random = new Random(Seed);

        var products = Catalogue
            .Select(item => Product.Create(
                item.Name,
                item.Price,
                random.Next(0, MaxSeedStock + 1)))
            .ToArray();

        db.Products.AddRange(products);
        await db.SaveChangesAsync(ct);
    }
}