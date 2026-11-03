using System.Net.Http.Json;

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using ProductManagement.Infrastructure.Persistence;
using ProductManagement.Infrastructure.Persistence.Identity;

using Testcontainers.PostgreSql;

namespace ProductManagement.Specs.Infrastructure;

/// <summary>
/// Owns the database container and the API host for the whole run.
/// Started once by the hooks, not per scenario.
/// </summary>
public static class SpecsFixture
{
    private static PostgreSqlContainer? _container;
    private static SpecsAppFactory? _factory;

    public static async Task StartAsync()
    {
        _container = new PostgreSqlBuilder("postgres:16-alpine")
            .WithDatabase("productmanagement")
            .WithUsername("postgres")
            .WithPassword("postgres")
            .Build();

        await _container.StartAsync();

        _factory = new SpecsAppFactory(_container.GetConnectionString());

        // Touching Services builds the host, which applies the config override.
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        await db.Database.MigrateAsync();
        await IdentitySeeder.SeedAsync(scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>());
    }

    public static async Task StopAsync()
    {
        if (_factory is not null)
        {
            await _factory.DisposeAsync();
        }

        if (_container is not null)
        {
            await _container.DisposeAsync();
        }
    }

    /// <summary>Clears products but keeps the seeded user, so login stays valid.</summary>
    public static async Task ResetAsync()
    {
        using var scope = _factory!.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        await db.Database.ExecuteSqlRawAsync("TRUNCATE \"Products\" RESTART IDENTITY;");
    }

    public static async Task<HttpClient> CreateAuthenticatedClientAsync()
    {
        var client = _factory!.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/login", new
        {
            email = IdentitySeeder.DemoEmail,
            password = IdentitySeeder.DemoPassword
        });

        response.EnsureSuccessStatusCode();

        return client;
    }
}