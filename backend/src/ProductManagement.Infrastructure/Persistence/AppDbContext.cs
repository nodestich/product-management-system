using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

using ProductManagement.Application.Common.Interfaces;
using ProductManagement.Domain.Models;
using ProductManagement.Infrastructure.Persistence.Identity;

namespace ProductManagement.Infrastructure.Persistence;

public class AppDbContext(DbContextOptions<AppDbContext> options)
    : IdentityUserContext<ApplicationUser, Guid>(options), IApplicationDbContext
{
    public DbSet<Product> Products => Set<Product>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        // Switched to first to configure identity tables.
        base.OnModelCreating(builder);

        // This automaticaly discovers and applies all configs instead of doing manually for each.
        builder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

        // Trigram matching for the product name search index.
        builder.HasPostgresExtension("pg_trgm");
    }
}