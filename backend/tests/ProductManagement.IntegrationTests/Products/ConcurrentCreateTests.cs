using System.Net;
using System.Net.Http.Json;

using ProductManagement.Application.Common.DTOs.Product;
using ProductManagement.IntegrationTests.Infrastructure;

namespace ProductManagement.IntegrationTests.Products;

[Collection(nameof(ApiCollectionFixture))]
public sealed class ConcurrentCreateTests(ApiFactory factory) : IAsyncLifetime
{
    public Task InitializeAsync() => factory.ResetDatabaseAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task ConcurrentCreates_ProduceUniqueSixDigitIds()
    {
        const int parallelism = 25;
        var client = factory.CreateClient();

        var requests = Enumerable.Range(0, parallelism)
            .Select(i => client.PostAsJsonAsync("/api/products", new CreateProductRequest($"P{i}", 1m, 1)))
            .ToArray();

        var responses = await Task.WhenAll(requests);

        Assert.All(responses, r => Assert.Equal(HttpStatusCode.Created, r.StatusCode));

        // 25 products = consecutive ids from the database sequence: 100000..100024
        var ids = await Task.WhenAll(responses.Select(async r =>
            (await r.Content.ReadFromJsonAsync<ProductDto>())!.Id));

        // As many IDs as oarallel operations
        Assert.Equal(parallelism, ids.Distinct().Count());
        Assert.All(ids, id => Assert.InRange(id, 100_000, 999_999));
        Assert.Equal(Enumerable.Range(100_000, parallelism), ids.Order());
    }
}