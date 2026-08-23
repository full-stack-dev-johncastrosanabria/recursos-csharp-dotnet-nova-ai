namespace Training.Module10.IntegrationTests;

/// <summary>One database for every class in this tier, which run in sequence.</summary>
[CollectionDefinition(Name)]
public sealed class SharedStockDatabase : ICollectionFixture<StockDatabase>
{
    public const string Name = "stock database";
}
