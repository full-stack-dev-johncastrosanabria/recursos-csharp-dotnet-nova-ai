namespace Training.Module11.IntegrationTests;

/// <summary>One container and one seeded schema for every class in this tier.</summary>
[CollectionDefinition(Name)]
public sealed class SharedShopDatabase : ICollectionFixture<ShopDatabase>
{
    public const string Name = "shop database";
}
