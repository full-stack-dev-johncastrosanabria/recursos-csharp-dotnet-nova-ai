using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Npgsql;
using Testcontainers.PostgreSql;
using Training.Module11.Core;

namespace Training.Module11.IntegrationTests;

/// <summary>Counts the SQL commands EF Core actually sends.</summary>
public sealed class CommandCounter : DbCommandInterceptor
{
    private int _count;

    public int Count => Volatile.Read(ref _count);

    public IList<string> Statements { get; } = [];

    public void Reset()
    {
        Interlocked.Exchange(ref _count, 0);
        Statements.Clear();
    }

    public override InterceptionResult<System.Data.Common.DbDataReader> ReaderExecuting(
        System.Data.Common.DbCommand command,
        CommandEventData eventData,
        InterceptionResult<System.Data.Common.DbDataReader> result)
    {
        Record(command);

        return result;
    }

    // Both halves are needed. Lazy loading resolves synchronously even inside
    // an async method, while every ToListAsync goes down the async path -- so
    // an interceptor that overrides only one of these counts a suspiciously
    // convenient subset of the truth.
    public override ValueTask<InterceptionResult<System.Data.Common.DbDataReader>> ReaderExecutingAsync(
        System.Data.Common.DbCommand command,
        CommandEventData eventData,
        InterceptionResult<System.Data.Common.DbDataReader> result,
        CancellationToken cancellationToken = default)
    {
        Record(command);

        return ValueTask.FromResult(result);
    }

    private void Record(System.Data.Common.DbCommand command)
    {
        Interlocked.Increment(ref _count);

        lock (Statements)
        {
            Statements.Add(command.CommandText);
        }
    }
}

/// <summary>
/// A real PostgreSQL 18 with a real schema, so the module's two claims can be
/// counted rather than argued about: how many round trips a loading strategy
/// costs, and whether a re-query returns what is in the database.
///
/// The SDK-only tier asks what SQL EF Core WOULD send, which needs no server.
/// This tier is where "would" becomes "did".
/// </summary>
public sealed class ShopDatabase : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:18").Build();

    public CommandCounter Commands { get; } = new();

    private string ConnectionString => _container.GetConnectionString();

    public async ValueTask InitializeAsync()
    {
        await _container.StartAsync();

        await using var db = Create();
        await db.Database.EnsureCreatedAsync();

        for (var i = 1; i <= 50; i++)
        {
            db.Orders.Add(new Order
            {
                Reference = $"ORD-{i}",
                CustomerEmail = $"User{i}@Example.com",
                Total = 100 + i,
                Lines =
                [
                    new OrderLine { Sku = "A", Quantity = 1 },
                    new OrderLine { Sku = "B", Quantity = 2 },
                    new OrderLine { Sku = "C", Quantity = 3 },
                ],
                Payments = [new Payment { Method = "card", Amount = 100 + i }],
            });
        }

        await db.SaveChangesAsync();

        // The 50 orders above carry children and are what the round-trip
        // counting tests measure. These 20,000 do not: they exist so the
        // planner has a table worth using an index on. At 50 rows PostgreSQL
        // scans whatever you ask it to, correctly -- which is module 09's
        // point, and would make a plan assertion here prove nothing.
        await using var bulk = new NpgsqlConnection(ConnectionString);
        await bulk.OpenAsync();
        await using var seed = new NpgsqlCommand(
            """
            INSERT INTO "Orders" ("Reference", "CustomerEmail", "Total")
            SELECT 'BULK-' || i, 'bulk' || i || '@Example.com', 10
            FROM generate_series(1, 20000) AS i;

            CREATE INDEX orders_reference_idx ON "Orders" ("Reference");
            ANALYZE "Orders";
            """, bulk);
        await seed.ExecuteNonQueryAsync();

        Commands.Reset();
    }

    public ValueTask DisposeAsync() => _container.DisposeAsync();

    /// <summary>A context over the container. Lazy loading is opt-in, exactly as in an application.</summary>
    public ShopContext Create(bool lazyLoading = false)
    {
        var builder = new DbContextOptionsBuilder<ShopContext>()
            .UseNpgsql(ConnectionString)
            .AddInterceptors(Commands);

        if (lazyLoading)
        {
            builder.UseLazyLoadingProxies();
        }

        return new ShopContext(builder.Options);
    }

    /// <summary>
    /// Runs EXPLAIN over a statement, substituting a literal for EF's parameter.
    ///
    /// Takes the RAW ToQueryString output rather than the whitespace-collapsed
    /// form the Core exercise produces: EF prefixes parameterised SQL with a
    /// "-- @__p='value'" declaration line, and collapsing the newlines turns
    /// that comment into one that swallows the entire statement.
    /// </summary>
    public async Task<string> ExplainAsync(string sql, string parameterValue, CancellationToken cancellationToken)
    {
        var withoutDeclarations = string.Join(
            '\n',
            sql.Split('\n').Where(line => !line.TrimStart().StartsWith("--", StringComparison.Ordinal)));

        var statement = System.Text.RegularExpressions.Regex.Replace(
            withoutDeclarations, @"@[A-Za-z_][A-Za-z0-9_]*", $"'{parameterValue}'");

        await using var connection = new NpgsqlConnection(ConnectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = new NpgsqlCommand($"EXPLAIN (ANALYZE, FORMAT JSON) {statement}", connection);

        return (string)(await command.ExecuteScalarAsync(cancellationToken))!;
    }
}
