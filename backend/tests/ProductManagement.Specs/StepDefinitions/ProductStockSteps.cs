using ProductManagement.Specs.Infrastructure;

using Reqnroll;

namespace ProductManagement.Specs.StepDefinitions;

[Binding]
public sealed class ProductStockSteps(ScenarioContext scenarioContext)
{
    private HttpClient Client => (HttpClient)scenarioContext[ScenarioKeys.Client];
    private int ProductId => (int)scenarioContext[ScenarioKeys.ProductId];

    [When(@"I decrease the stock by (.*)")]
    public async Task WhenIDecreaseTheStockBy(int quantity)
    {
        var response = await Client.PostAsync(
            $"/api/products/{ProductId}/decrement-stock/{quantity}",
            content: null);

        response.EnsureSuccessStatusCode();
    }

    [When(@"I try to decrease the stock by (.*)")]
    public async Task WhenITryToDecreaseTheStockBy(int quantity) =>
        await TryDecrement(ProductId, quantity);

    [When(@"I try to decrease the stock of product (.*)")]
    public async Task WhenITryToDecreaseTheStockOfProduct(int productId) =>
        await TryDecrement(productId, quantity: 1);

    [When(@"I try to increase the stock by (.*)")]
    public async Task WhenITryToIncreaseTheStockBy(int quantity)
    {
        scenarioContext[ScenarioKeys.LastResponse] = await Client.PostAsync(
            $"/api/products/{ProductId}/add-to-stock/{quantity}",
            content: null);
    }

    private async Task TryDecrement(int productId, int quantity)
    {
        scenarioContext[ScenarioKeys.LastResponse] = await Client.PostAsync(
            $"/api/products/{productId}/decrement-stock/{quantity}",
            content: null);
    }
}