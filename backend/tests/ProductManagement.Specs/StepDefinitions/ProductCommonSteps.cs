using System.Net;
using System.Net.Http.Json;

using ProductManagement.Application.DTOs.Product;
using ProductManagement.Specs.Infrastructure;

using Reqnroll;

namespace ProductManagement.Specs.StepDefinitions;

/// <summary>
/// Steps shared by more than one feature. Reqnroll keeps the step vocabulary
/// global, so a phrase may only be defined once in the whole assembly.
/// </summary>
[Binding]
public sealed class ProductCommonSteps(ScenarioContext scenarioContext)
{
    private HttpClient Client => (HttpClient)scenarioContext[ScenarioKeys.Client];

    [Given(@"a product named ""(.*)"" with (.*) items in stock")]
    public async Task GivenAProductWithStock(string name, int stock)
    {
        var response = await Client.PostAsJsonAsync(
            "/api/products",
            new CreateProductRequest(name, 1m, stock));

        response.EnsureSuccessStatusCode();

        var product = await response.Content.ReadFromJsonAsync<ProductDto>();

        scenarioContext[ScenarioKeys.ProductId] = product!.Id;
        scenarioContext[ScenarioKeys.LastResponse] = response;
        scenarioContext[ScenarioKeys.LastProduct] = product;
    }

    [Then(@"the stock is (.*)")]
    public async Task ThenTheStockIs(int expected)
    {
        var productId = (int)scenarioContext[ScenarioKeys.ProductId];

        var product = await Client.GetFromJsonAsync<ProductDto>($"/api/products/{productId}");

        Assert.Equal(expected, product!.Stock);
    }

    [Then(@"the request fails with status (.*)")]
    public void ThenTheRequestFailsWithStatus(int statusCode)
    {
        var response = (HttpResponseMessage)scenarioContext[ScenarioKeys.LastResponse];

        Assert.Equal((HttpStatusCode)statusCode, response.StatusCode);
    }
}