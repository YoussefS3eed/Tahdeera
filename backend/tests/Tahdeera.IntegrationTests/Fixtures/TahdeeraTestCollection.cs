namespace Tahdeera.IntegrationTests.Fixtures;

/// <summary>
/// xUnit collection definition for all integration tests.
/// Ensures all test classes decorated with [Collection("IntegrationTests")]
/// share a single instance of CustomWebApplicationFactory and a single PostgreSQL 18 container.
/// </summary>
[CollectionDefinition("IntegrationTests")]
public class TahdeeraTestCollection : ICollectionFixture<CustomWebApplicationFactory>
{
    // Marker class for xUnit collection fixture. No code required.
}
