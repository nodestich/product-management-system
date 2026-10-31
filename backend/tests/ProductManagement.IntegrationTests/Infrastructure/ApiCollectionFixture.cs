namespace ProductManagement.IntegrationTests.Infrastructure;

[CollectionDefinition(nameof(ApiCollectionFixture))]
public sealed class ApiCollectionFixture : ICollectionFixture<ApiFactory>;

// Tests in a collection run sequentially, they share 1 database.