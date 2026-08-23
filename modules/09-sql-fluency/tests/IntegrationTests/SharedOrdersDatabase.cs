namespace Training.Module09.IntegrationTests;

/// <summary>
/// One container for every class in this tier. Classes in a collection run one
/// after another, which also makes it safe for a test to create an index and
/// drop it again.
/// </summary>
[CollectionDefinition(Name)]
public sealed class SharedOrdersDatabase : ICollectionFixture<OrdersDatabase>
{
    public const string Name = "orders database";
}
