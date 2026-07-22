namespace Integration.Tests.utils;

/// <summary>
/// Shared collection that keeps Allure history/trends coherent across test
/// classes that would otherwise run in parallel. Test classes opt in with
/// [Collection("Allure History Collection")], mirroring the Keystone suite.
/// </summary>
[CollectionDefinition("Allure History Collection")]
public sealed class AllureHistoryCollection : ICollectionFixture<AllureHistoryFixture>;

public sealed class AllureHistoryFixture
{
    // Marker fixture - presence is enough to serialize the collection.
}
