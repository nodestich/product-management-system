using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using ProductManagement.Domain.Models;

namespace ProductManagement.Infrastructure.Persistence;

public sealed class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        // Adding some constraints to keep data integrity.
        builder.ToTable("Products", table =>
       {
           table.HasCheckConstraint("CK_Products_Id_SixDigits", "\"Id\" BETWEEN 100000 AND 999999");
           table.HasCheckConstraint("CK_Products_Price_NonNegative", "\"Price\" >= 0");
           table.HasCheckConstraint("CK_Products_Stock_Range", $"\"Stock\" >= 0 AND \"Stock\" <= {Product.MaxStock}");
       });

        builder.HasKey(p => p.Id);

        // Delegate the responsability of generating the Id to the database,
        // but we can still set a range for it.
        builder.Property(p => p.Id)
            .UseIdentityByDefaultColumn()
            .HasIdentityOptions(startValue: 100_000L, maxValue: 999_999L);

        builder.Property(p => p.Name)
            .IsRequired()
            .HasMaxLength(Product.MaxNameLength);

        builder.Property(p => p.Price)
            .HasPrecision(18, 2);

        builder.Property(p => p.CreatedAt).HasColumnType("timestamp with time zone");
        builder.Property(p => p.UpdatedAt).HasColumnType("timestamp with time zone");

        // Name is only ever queried with a case-insensitive "contains" search
        // (lower("Name") LIKE '%term%'), which a btree index on "Name" cannot serve.
        // It is backed by a pg_trgm GIN index on lower("Name") created in the
        // AddProductNameTrigramIndex migration: EF Core cannot model expression indexes.
    }
}