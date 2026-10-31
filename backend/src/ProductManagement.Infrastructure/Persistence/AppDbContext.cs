using Microsoft.EntityFrameworkCore;

using ProductManagement.Application.Common.Interfaces;
using ProductManagement.Domain.Models;

namespace ProductManagement.Infrastructure.Persistence;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options), IApplicationDbContext
{
    public DbSet<Product> Products => Set<Product>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // This automaticaly discovers and applies all configs instead of doing manually for each.
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}