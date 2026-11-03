using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace ProductManagement.Specs.Infrastructure;

/// <summary>
/// Hosts the real API in memory. Only the connection string is overridden, so
/// every scenario exercises the same pipeline a real request would.
/// </summary>
public sealed class SpecsAppFactory(string connectionString) : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // "Testing" keeps the Development seeder and Scalar out of the way.
        builder.UseEnvironment("Testing");

        builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(
            new Dictionary<string, string?>
            {
                ["ConnectionStrings:Default"] = connectionString
            }));
    }
}