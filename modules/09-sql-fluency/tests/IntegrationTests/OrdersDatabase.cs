using Npgsql;
using Testcontainers.PostgreSql;

namespace Training.Module09.IntegrationTests;

/// <summary>
/// A real PostgreSQL 18, started once for the whole class, seeded with the same
/// shape of data the captured plans in the unit tier came from.
///
/// The unit tier proves you can read a plan. This tier proves the plans were
/// worth reading: the same analyser, pointed at output the planner produces
/// right now, on this machine, against real indexes and real statistics. If a
/// rule in the unit tests only held because of how a fixture was captured, it
/// fails here.
/// </summary>
public sealed class OrdersDatabase : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:18").Build();

    private NpgsqlDataSource? _dataSource;

    public async ValueTask InitializeAsync()
    {
        await _container.StartAsync();
        _dataSource = NpgsqlDataSource.Create(_container.GetConnectionString());

        await ExecuteAsync("""
            CREATE TABLE orders (
              id             bigserial PRIMARY KEY,
              customer_email text        NOT NULL,
              status         text        NOT NULL,
              placed_at      timestamptz NOT NULL,
              total_cents    integer     NOT NULL
            );

            INSERT INTO orders (customer_email, status, placed_at, total_cents)
            SELECT
              'User' || (i % 12500) || '@Example.com',
              (ARRAY['placed','paid','shipped','cancelled'])[1 + (i % 4)],
              timestamptz '2025-01-01 00:00:00+00' + (i % 730) * interval '1 day',
              100 + (i % 90000)
            FROM generate_series(1, 50000) AS i;

            CREATE INDEX orders_customer_email_idx ON orders (customer_email);
            CREATE INDEX orders_placed_at_idx ON orders (placed_at);
            ANALYZE orders;
            """);
    }

    public async ValueTask DisposeAsync()
    {
        if (_dataSource is not null)
        {
            await _dataSource.DisposeAsync();
        }

        await _container.DisposeAsync();
    }

    /// <summary>Runs EXPLAIN (ANALYZE, FORMAT JSON) and returns the raw JSON.</summary>
    public async Task<string> ExplainAsync(string sql, CancellationToken cancellationToken)
    {
        await using var command = _dataSource!.CreateCommand(
            $"EXPLAIN (ANALYZE, COSTS, TIMING, FORMAT JSON) {sql}");

        var json = await command.ExecuteScalarAsync(cancellationToken);

        return (string)json!;
    }

    public async Task ExecuteAsync(string sql, CancellationToken cancellationToken = default)
    {
        await using var command = _dataSource!.CreateCommand(sql);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }
}
