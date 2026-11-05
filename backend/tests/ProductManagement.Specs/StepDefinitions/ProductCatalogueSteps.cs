using System.Globalization;
using System.Net;
using System.Net.Http.Json;

using ProductManagement.Application.DTOs.Product;
using ProductManagement.Application.Common.Pagination;
using ProductManagement.Specs.Infrastructure;

using Reqnroll;

namespace ProductManagement.Specs.StepDefinitions;

[Binding]
public sealed class ProductCatalogueSteps(ScenarioContext scenarioContext)
{
    private HttpClient Client => (HttpClient)scenarioContext[ScenarioKeys.Client];
    private int ProductId => (int)scenarioContext[ScenarioKeys.ProductId];

    [When(@"I create a product named ""(.*)"" priced (.*) with (.*) in stock")]
    public async Task WhenICreateAProduct(string name, string price, int stock)
    {
        var response = await Client.PostAsJsonAsync(
            "/api/products",
            new CreateProductRequest(name, ParsePrice(price), stock));

        // Read the body once: the content stream cannot be read twice.
        var product = await response.Content.ReadFromJsonAsync<ProductDto>();

        scenarioContext[ScenarioKeys.LastResponse] = response;
        scenarioContext[ScenarioKeys.LastProduct] = product;
        scenarioContext[ScenarioKeys.ProductId] = product!.Id;
    }

    [When(@"I rename it to ""(.*)"" priced (.*)")]
    public async Task WhenIRenameItTo(string name, string price)
    {
        var response = await Client.PutAsJsonAsync(
            $"/api/products/{ProductId}",
            new UpdateProductRequest(name, ParsePrice(price)));

        response.EnsureSuccessStatusCode();
    }

    [When(@"I search for ""(.*)""")]
    public async Task WhenISearchFor(string name)
    {
        scenarioContext[ScenarioKeys.SearchResult] = await Client
            .GetFromJsonAsync<PagedResult<ProductDto>>(
                $"/api/products/search?name={Uri.EscapeDataString(name)}");
    }

    [When(@"I search for ""(.*)"" on page (.*) with page size (.*)")]
    public async Task WhenISearchWithPagination(string name, int page, int pageSize)
    {
        scenarioContext[ScenarioKeys.LastResponse] = await Client.GetAsync(
            $"/api/products/search?name={Uri.EscapeDataString(name)}&page={page}&pageSize={pageSize}");
    }

    [Then(@"the response is (.*)")]
    public void ThenTheResponseIs(int statusCode)
    {
        var response = (HttpResponseMessage)scenarioContext[ScenarioKeys.LastResponse];

        Assert.Equal((HttpStatusCode)statusCode, response.StatusCode);
    }

    [Then(@"the product identifier has (.*) digits")]
    public void ThenTheProductIdentifierHasDigits(int digits)
    {
        var product = (ProductDto)scenarioContext[ScenarioKeys.LastProduct];

        var length = product.Id.ToString(CultureInfo.InvariantCulture).Length;

        Assert.Equal(digits, length);
    }

    [Then(@"the name is ""(.*)""")]
    public async Task ThenTheNameIs(string expected)
    {
        var product = await Client.GetFromJsonAsync<ProductDto>($"/api/products/{ProductId}");

        Assert.Equal(expected, product!.Name);
    }

    [Then(@"the search returns (.*) results?")]
    public void ThenTheSearchReturns(int expected)
    {
        var result = (PagedResult<ProductDto>)scenarioContext[ScenarioKeys.SearchResult];

        Assert.Equal(expected, result.Items.Count);
    }

    private static decimal ParsePrice(string value) =>
        decimal.Parse(value, NumberStyles.Number, CultureInfo.InvariantCulture);
}