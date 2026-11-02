using System.Net;
using System.Net.Http.Json;

using ProductManagement.Application.Common.DTOs.Product;
using ProductManagement.IntegrationTests.Infrastructure;
using ProductManagement.Infrastructure.Persistence.Identity;

namespace ProductManagement.IntegrationTests.Auth;

[Collection(nameof(ApiCollectionFixture))]
public sealed class AuthEndpointsTests(ApiFactory factory)
{
    [Fact]
    public async Task Reads_stay_anonymous()
    {
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/products");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Write_without_login_returns401()
    {
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/products", new CreateProductRequest("Unauthorised", 1m, 1));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_with_wrong_password_returns401()
    {
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/login", new
        {
            email = IdentitySeeder.DemoEmail,
            password = "not-the-password"
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_returns_current_user_and_sets_cookie()
    {
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/login", new
        {
            email = IdentitySeeder.DemoEmail,
            password = IdentitySeeder.DemoPassword
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<CurrentUser>();
        Assert.Equal(IdentitySeeder.DemoEmail, body!.Email);

        Assert.Contains(
            response.Headers.GetValues("Set-Cookie"),
            cookie => cookie.StartsWith(".AspNetCore.Identity.Application", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Me_requires_authentication()
    {
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/auth/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Logout_clears_the_cookie()
    {
        using var client = await factory.CreateAuthenticatedClientAsync();

        var before = await client.GetAsync("/api/auth/me");
        Assert.Equal(HttpStatusCode.OK, before.StatusCode);

        var logout = await client.PostAsync("/api/auth/logout", null);
        Assert.Equal(HttpStatusCode.NoContent, logout.StatusCode);

        var after = await client.GetAsync("/api/auth/me");
        Assert.Equal(HttpStatusCode.Unauthorized, after.StatusCode);
    }

    private sealed record CurrentUser(Guid Id, string Email);
}
