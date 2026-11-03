using System.Net;
using System.Net.Http.Json;

using ProductManagement.Application.Common.DTOs.Product;
using ProductManagement.Application.Common.Pagination;
using ProductManagement.IntegrationTests.Infrastructure;

namespace ProductManagement.IntegrationTests.Products;

[Collection(nameof(ApiCollectionFixture))]
public sealed class PaginationTests(ApiFactory factory) : IAsyncLifetime
{
    private readonly HttpClient _client = factory.CreateClient();

    public Task InitializeAsync() => factory.ResetDatabaseAsync();

    public Task DisposeAsync()
    {
        _client.Dispose();
        return Task.CompletedTask;
    }

    /// <summary>
    /// Creates products that all share the same UpdatedAt, so any test asserting
    /// page stability also proves the Id tiebreaker in the ordering.
    /// </summary>
    private async Task SeedAsync(int count, int stock = 5)
    {
        using var authClient = await factory.CreateAuthenticatedClientAsync();

        for (var i = 0; i < count; i++)
        {
            var response = await authClient.PostAsJsonAsync(
                "/api/products",
                new CreateProductRequest($"Item {i:00}", 1m, stock));

            response.EnsureSuccessStatusCode();
        }
    }

    [Fact]
    public async Task Search_returns_requested_page_with_total_count()
    {
        await SeedAsync(5);

        var page = await _client.GetFromJsonAsync<PagedResult<ProductDto>>(
            "/api/products/search?name=Item&page=2&pageSize=2");

        Assert.NotNull(page);
        Assert.Equal(2, page.Items.Count);
        Assert.Equal(5, page.TotalCount);
        Assert.Equal(3, page.TotalPages);
        Assert.Equal(2, page.Page);
        Assert.Equal(2, page.PageSize);
        Assert.True(page.HasPrevious);
        Assert.True(page.HasNext);
    }

    [Fact]
    public async Task Search_last_page_reports_no_next_page()
    {
        await SeedAsync(5);

        var page = await _client.GetFromJsonAsync<PagedResult<ProductDto>>(
            "/api/products/search?name=Item&page=3&pageSize=2");

        Assert.NotNull(page);
        Assert.Single(page.Items);
        Assert.False(page.HasNext);
        Assert.True(page.HasPrevious);
    }

    [Fact]
    public async Task Search_pages_do_not_overlap_or_skip_rows()
    {
        await SeedAsync(6);

        var first = await _client.GetFromJsonAsync<PagedResult<ProductDto>>(
            "/api/products/search?name=Item&page=1&pageSize=3");

        var second = await _client.GetFromJsonAsync<PagedResult<ProductDto>>(
            "/api/products/search?name=Item&page=2&pageSize=3");

        var ids = first!.Items.Concat(second!.Items).Select(p => p.Id).ToList();

        Assert.Equal(6, ids.Count);
        Assert.Equal(6, ids.Distinct().Count());
    }

    [Fact]
    public async Task Search_page_beyond_the_end_returns_empty_items_but_keeps_total()
    {
        await SeedAsync(3);

        var page = await _client.GetFromJsonAsync<PagedResult<ProductDto>>(
            "/api/products/search?name=Item&page=5&pageSize=2");

        Assert.NotNull(page);
        Assert.Empty(page.Items);
        Assert.Equal(3, page.TotalCount);
    }

    [Fact]
    public async Task StockLevel_returns_requested_page_with_total_count()
    {
        await SeedAsync(6);

        var page = await _client.GetFromJsonAsync<PagedResult<ProductDto>>(
            "/api/products/stock-level?min=0&max=10&page=2&pageSize=4");

        Assert.NotNull(page);
        Assert.Equal(2, page.Items.Count);
        Assert.Equal(6, page.TotalCount);
        Assert.Equal(2, page.TotalPages);
        Assert.False(page.HasNext);
    }

    [Fact]
    public async Task StockLevel_pages_do_not_overlap_or_skip_rows()
    {
        await SeedAsync(6);

        var first = await _client.GetFromJsonAsync<PagedResult<ProductDto>>(
            "/api/products/stock-level?min=0&max=10&page=1&pageSize=3");

        var second = await _client.GetFromJsonAsync<PagedResult<ProductDto>>(
            "/api/products/stock-level?min=0&max=10&page=2&pageSize=3");

        var ids = first!.Items.Concat(second!.Items).Select(p => p.Id).ToList();

        Assert.Equal(6, ids.Count);
        Assert.Equal(6, ids.Distinct().Count());
    }

    [Theory]
    [InlineData("/api/products/search?name=Item&page=0")]
    [InlineData("/api/products/search?name=Item&page=-1")]
    [InlineData("/api/products/search?name=Item&pageSize=0")]
    [InlineData("/api/products/search?name=Item&pageSize=51")]
    [InlineData("/api/products/stock-level?min=0&max=10&page=0")]
    [InlineData("/api/products/stock-level?min=0&max=10&pageSize=51")]
    public async Task Invalid_pagination_returns400(string url)
    {
        var response = await _client.GetAsync(url);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Default_page_size_is_used_when_not_specified()
    {
        await SeedAsync(12);

        var page = await _client.GetFromJsonAsync<PagedResult<ProductDto>>(
            "/api/products/search?name=Item");

        Assert.NotNull(page);
        Assert.Equal(PaginationConfig.DefaultPageSize, page.PageSize);
        Assert.Equal(PaginationConfig.DefaultPageSize, page.Items.Count);
        Assert.Equal(12, page.TotalCount);
    }

    [Fact]
    public async Task GetAll_is_not_paginated()
    {
        await SeedAsync(12);

        var products = await _client.GetFromJsonAsync<List<ProductDto>>("/api/products");

        Assert.NotNull(products);
        Assert.Equal(12, products.Count);
    }
}
