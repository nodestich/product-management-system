namespace ProductManagement.Specs.Infrastructure;

/// <summary>
/// ScenarioContext keys. Centralised so a typo fails at compile time instead of
/// producing a runtime "key not found".
/// </summary>
public static class ScenarioKeys
{
    public const string Client = "client";
    public const string ProductId = "productId";
    public const string LastResponse = "lastResponse";
    public const string LastProduct = "lastProduct";
    public const string SearchResult = "searchResult";
}