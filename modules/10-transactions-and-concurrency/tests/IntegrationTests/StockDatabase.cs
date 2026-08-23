using System.Data;
using Npgsql;
using Testcontainers.PostgreSql;

namespace Training.Module10.IntegrationTests;

/// <summary>
/// A real PostgreSQL 18 with one row of stock in it.
///
/// Concurrency is the one subject that cannot be learned from a captured
/// artifact. Module 09 could ship real plans in files because a plan is a
/// document; a race is an event, and the only honest way to show one is to run
/// it. Everything in this tier races for real.
/// </summary>
public sealed class StockDatabase : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container =
        new PostgreSqlBuilder("postgres:18").Build();

    public string ConnectionString => _container.GetConnectionString();

    public async ValueTask InitializeAsync()
    {
        await _container.StartAsync();
        await ExecuteAsync("""
            CREATE TABLE stock (
              sku      text PRIMARY KEY,
              quantity int  NOT NULL,
              version  int  NOT NULL DEFAULT 0
            );
            """);
    }

    public ValueTask DisposeAsync() => _container.DisposeAsync();

    /// <summary>Puts a single SKU back to a known quantity before a race.</summary>
    public async Task ResetAsync(string sku, int quantity, CancellationToken cancellationToken)
    {
        await ExecuteAsync(
            $"""
             DELETE FROM stock WHERE sku = '{sku}';
             INSERT INTO stock (sku, quantity) VALUES ('{sku}', {quantity});
             """,
            cancellationToken);
    }

    public async Task<int> QuantityAsync(string sku, CancellationToken cancellationToken)
    {
        await using var connection = new NpgsqlConnection(ConnectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = new NpgsqlCommand("SELECT quantity FROM stock WHERE sku = @sku", connection);
        command.Parameters.AddWithValue("sku", sku);

        return (int)(await command.ExecuteScalarAsync(cancellationToken))!;
    }

    public async Task ExecuteAsync(string sql, CancellationToken cancellationToken = default)
    {
        await using var connection = new NpgsqlConnection(ConnectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = new NpgsqlCommand(sql, connection);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    /// <summary>The SQLSTATE a piece of work produced, or null if it succeeded.</summary>
    public static async Task<string?> SqlStateOfAsync(Func<Task> work)
    {
        try
        {
            await work();

            return null;
        }
        catch (PostgresException failure)
        {
            return failure.SqlState;
        }
    }

    /// <summary>The isolation level the server says it is actually running at.</summary>
    public async Task<string> ReportedIsolationAsync(IsolationLevel requested, CancellationToken cancellationToken)
    {
        await using var connection = new NpgsqlConnection(ConnectionString);
        await connection.OpenAsync(cancellationToken);

        await using var transaction = await connection.BeginTransactionAsync(requested, cancellationToken);
        await using var command = new NpgsqlCommand("SHOW transaction_isolation", connection, transaction);

        return (string)(await command.ExecuteScalarAsync(cancellationToken))!;
    }
}
