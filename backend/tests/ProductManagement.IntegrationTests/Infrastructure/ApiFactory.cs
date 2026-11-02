using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using ProductManagement.Infrastructure.Persistence;
using ProductManagement.Infrastructure.Persistence.Identity;

using System.Net.Http.Json;

using Testcontainers.PostgreSql;

namespace ProductManagement.IntegrationTests.Infrastructure;

public sealed class ApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{

    // Running a testcontainer instead of InMemory because we need to test some cases that
    // an InMemory DB wouldnt be able to catch, more precisely:
    // UseIdentityByDefaultColumn (responsible for keeping IDs in order)
    private readonly PostgreSqlContainer _db = new PostgreSqlBuilder("postgres:16-alpine")
        .WithDatabase("productmanagement")
        .WithUsername("postgres")
        .WithPassword("postgres")
        .Build();

    public async Task InitializeAsync()
    {
        await _db.StartAsync();

        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        // Run the real migrations
        await db.Database.MigrateAsync();

        // The host runs as "Testing", so the API's Development seeding never runs here.
        await IdentitySeeder.SeedAsync(scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>());
    }

    /// <summary>
    /// Returns a client that has logged in. The default handler keeps cookies, so the
    /// identity cookie set by /api/auth/login is sent on subsequent write requests.
    /// </summary>
    public async Task<HttpClient> CreateAuthenticatedClientAsync()
    {
        var client = CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/login", new
        {
            email = IdentitySeeder.DemoEmail,
            password = IdentitySeeder.DemoPassword
        });

        response.EnsureSuccessStatusCode();

        return client;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // This makes the IsDevelopment() false
        // Basically here so that the seeder doesnt run and we have a fresh DB
        builder.UseEnvironment("Testing");

        builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(
            new Dictionary<string, string?>
            {
                ["ConnectionStrings:Default"] = _db.GetConnectionString()
            }));
    }

    public async Task ResetDatabaseAsync()
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        // This restarts the ID sequence to 100000, referent to our ID constraing/requirement.
        // TODO-JS: Once we have a relational entity we would need this truncate to work in cascade.
        await db.Database.ExecuteSqlRawAsync("TRUNCATE \"Products\" RESTART IDENTITY;");
    }

    async Task IAsyncLifetime.DisposeAsync() => await _db.DisposeAsync();
}