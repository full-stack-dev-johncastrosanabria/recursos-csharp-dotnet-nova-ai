namespace Training.Module09.IntegrationTests;

/// <summary>One SQL Server container shared by every class that needs one.</summary>
[CollectionDefinition(Name)]
public sealed class SharedSqlServer : ICollectionFixture<SqlServerOrders>
{
    public const string Name = "sql server orders";
}
