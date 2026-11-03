using ProductManagement.Specs.Infrastructure;

using Reqnroll;

namespace ProductManagement.Specs.Infrastructure;

[Binding]
public sealed class Hooks
{
    [BeforeTestRun]
    public static Task BeforeTestRun() => SpecsFixture.StartAsync();

    [AfterTestRun]
    public static Task AfterTestRun() => SpecsFixture.StopAsync();

    [BeforeScenario]
    public static async Task BeforeScenario(ScenarioContext scenarioContext)
    {
        await SpecsFixture.ResetAsync();

        scenarioContext[ScenarioKeys.Client] = await SpecsFixture.CreateAuthenticatedClientAsync();
    }

    [AfterScenario]
    public static void AfterScenario(ScenarioContext scenarioContext)
    {
        if (scenarioContext.TryGetValue(ScenarioKeys.Client, out HttpClient? client) && client is not null)
        {
            client.Dispose();
        }
    }
}