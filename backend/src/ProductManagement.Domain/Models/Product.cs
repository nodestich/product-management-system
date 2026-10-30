using ProductManagement.Domain.Common;

namespace ProductManagement.Domain.Models;

public class Product : BaseEntity
{
    public int Id { get; private set; }
    public string Name { get; private set; } = null!;
    public decimal Price { get; private set; }
    public int Stock { get; private set; }

    public const int MinNameLength = 2;
    public const int MaxNameLength = 100;
    public const int MaxStock = 1000; // Assumption


    // Keep for EF core materialisation
    private Product()
    {

    }

    public static Product Create(
        string name,
        decimal price,
        int stock)
    {
        var dateNow = DateTime.UtcNow;

        var validName = FormatAndValidateName(name);
        var validPrice = ValidatePrice(price);

        return new Product
        {
            Name = validName,
            Price = validPrice,
            Stock = ValidateStock(stock),
            CreatedAt = dateNow,
            UpdatedAt = dateNow
        };

    }

    public void Update(
        string name,
        decimal price)
    {
        Name = FormatAndValidateName(name);
        Price = ValidatePrice(price);
        UpdatedAt = DateTime.UtcNow;
    }

    private static string FormatAndValidateName(string? n)
    {
        if (string.IsNullOrWhiteSpace(n))
        {
            throw new DomainException("Product name cannot be empty.");
        }

        n = n.Trim(); // ran after the whitespace check

        return n.Length is < MinNameLength or > MaxNameLength
            ? throw new DomainException($"Product name must be between {MinNameLength} and {MaxNameLength} characters.")
            : n;
    }

    private static decimal ValidatePrice(decimal p)
    {
        return p < 0
            ? throw new DomainException("Product price cannot be negative.")
            : p;
    }

    private static int ValidateStock(int s)
    {
        if (s < 0)
        {
            throw new DomainException("Product stock cannot be negative.");
        }
        else if (s > MaxStock)
        {
            throw new DomainException("Product exceeds maximum stock.");
        }

        return s;
    }
}