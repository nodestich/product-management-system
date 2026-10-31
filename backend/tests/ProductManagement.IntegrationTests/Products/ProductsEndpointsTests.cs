using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

using ProductManagement.Application.Common.DTOs.Product;
using ProductManagement.IntegrationTests.Infrastructure;

namespace ProductManagement.IntegrationTests.Products;

[Collection(nameof(ApiCollectionFixture))]
public sealed class ProductEndpointsTests : IAsyncLifetime
{
    private readonly ApiFactory _factory;
    private readonly HttpClient _client;

    public ProductEndpointsTests(ApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    public Task InitializeAsync()
    {
        // Runs before each test, otherwise state would leak
        return _factory.ResetDatabaseAsync();
    }

    public Task DisposeAsync()
    {
        // Runs after each test
        _client.Dispose();
        return Task.CompletedTask;
    }

    private static CreateProductRequest Valid(string name = "Monitor") => new(name, 99.99m, 10);

    private async Task<ProductDto> CreateAsync(string name = "Monitor", int stock = 10)
    {
        var response = await _client.PostAsJsonAsync("/api/products", new CreateProductRequest(name, 99.99m, stock));
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<ProductDto>())!;
    }

    [Theory]
    [InlineData("", 1.0, 1)]
    [InlineData("Valid", -1.0, 1)]
    [InlineData("Valid", 1.0, -1)]
    public async Task Create_WithInvalidValues_Returns400(string name, double price, int stock)
    {
        var response = await _client.PostAsJsonAsync("/api/products",
            new CreateProductRequest(name, (decimal)price, stock));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetAll_WhenEmpty_ReturnsEmptyArray()
    {
        var products = await _client.GetFromJsonAsync<List<ProductDto>>("/api/products");
        Assert.Empty(products!);
    }

    [Fact]
    public async Task Create_Valid_Returns201SixDigitIdAndLocation()
    {
        var response = await _client.PostAsJsonAsync("/api/products", Valid());

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var dto = (await response.Content.ReadFromJsonAsync<ProductDto>())!;
        Assert.InRange(dto.Id, 100_000, 999_999);
        Assert.Equal(10, dto.Stock);

        var location = response.Headers.Location;

        Assert.NotNull(location);
        Assert.Equal($"/api/products/{dto.Id}", location!.PathAndQuery);
    }

    [Fact]
    public async Task GetById_Missing_Returns404ProblemDetails()
    {
        var response = await _client.GetAsync("/api/products/999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(404, problem.GetProperty("status").GetInt32());
        Assert.True(problem.TryGetProperty("traceId", out _));
    }

    [Fact]
    public async Task Update_Existing_PersistsChangesAndKeepsStock()
    {
        var created = await CreateAsync(stock: 7);

        var response = await _client.PutAsJsonAsync($"/api/products/{created.Id}",
            new UpdateProductRequest("Renamed", 5.55m));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var updated = (await response.Content.ReadFromJsonAsync<ProductDto>())!;
        Assert.Equal("Renamed", updated.Name);
        Assert.Equal(5.55m, updated.Price);
        Assert.Equal(7, updated.Stock); // stock unchanged
        Assert.True(updated.UpdatedAt >= updated.CreatedAt);
    }

    [Fact]
    public async Task Delete_Existing_Returns204Then404()
    {
        var created = await CreateAsync();

        var first = await _client.DeleteAsync($"/api/products/{created.Id}");
        var second = await _client.DeleteAsync($"/api/products/{created.Id}");

        Assert.Equal(HttpStatusCode.NoContent, first.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, second.StatusCode);
    }

    [Fact]
    public async Task DecrementStock_ReducesStock()
    {
        var created = await CreateAsync(stock: 10);

        var response = await _client.PostAsync($"/api/products/{created.Id}/decrement-stock/4", null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var dto = (await response.Content.ReadFromJsonAsync<ProductDto>())!;
        Assert.Equal(6, dto.Stock);
    }

    [Fact]
    public async Task DecrementStock_Insufficient_Returns409()
    {
        var created = await CreateAsync(stock: 3);
        var response = await _client.PostAsync($"/api/products/{created.Id}/decrement-stock/4", null);
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task DecrementStock_Zero_Returns400()
    {
        var created = await CreateAsync(stock: 3);
        var response = await _client.PostAsync($"/api/products/{created.Id}/decrement-stock/0", null);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task DecrementStock_MissingProduct_Returns404()
    {
        var response = await _client.PostAsync("/api/products/999/decrement-stock/1", null);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task AddToStock_IncreasesStock()
    {
        var created = await CreateAsync(stock: 5);
        var response = await _client.PostAsync($"/api/products/{created.Id}/add-to-stock/3", null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var dto = (await response.Content.ReadFromJsonAsync<ProductDto>())!;
        Assert.Equal(8, dto.Stock);
    }

    [Fact]
    public async Task Search_IsCaseInsensitiveAndPartial()
    {
        await CreateAsync("Monitor 32 inches");
        await CreateAsync("Mechanical keyboard");

        var results = await _client.GetFromJsonAsync<List<ProductDto>>("/api/products/search?name=MONITOR");

        Assert.Single(results!);
        Assert.StartsWith("Monitor", results![0].Name);
    }

    [Fact]
    public async Task Search_Blank_Returns400()
    {
        var response = await _client.GetAsync("/api/products/search");
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task StockLevel_UsesInclusiveBounds()
    {
        await CreateAsync("AAA", stock: 3);
        await CreateAsync("BBB", stock: 7);
        await CreateAsync("CCC", stock: 12);

        var results = await _client.GetFromJsonAsync<List<ProductDto>>("/api/products/stock-level?min=3&max=12");

        Assert.Equal(3, results!.Count);
    }

    [Fact]
    public async Task StockLevel_Inverted_Returns400()
    {
        var response = await _client.GetAsync("/api/products/stock-level?min=10&max=5");
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Timestamps_SerializeWithUtcSuffix()
    {
        var created = await CreateAsync();

        var json = await _client.GetStringAsync($"/api/products/{created.Id}");

        Assert.Matches("\"createdAt\":\"[^\"]+Z\"", json);
    }
}