using Npgsql;
using Training.Module10.Core;

namespace Training.Module10.IntegrationTests;

/// <summary>
/// The learner's IStockStore, backed by a real table.
///
/// UpdateIfVersionMatchesAsync is the whole optimistic scheme in one statement:
/// the version is in the WHERE clause, so a row somebody else has moved simply
/// does not match, and the command reports zero rows affected.
/// </summary>
public sealed class PostgresStockStore(string connectionString) : IStockStore
{
    public async Task<StockRow?> ReadAsync(string sku)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();

        await using var command = new NpgsqlCommand(
            "SELECT sku, quantity, version FROM stock WHERE sku = @sku", connection);
        command.Parameters.AddWithValue("sku", sku);

        await using var reader = await command.ExecuteReaderAsync();

        return await reader.ReadAsync()
            ? new StockRow(reader.GetString(0), reader.GetInt32(1), reader.GetInt32(2))
            : null;
    }

    public async Task<int> UpdateIfVersionMatchesAsync(string sku, int quantity, int expectedVersion)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();

        await using var command = new NpgsqlCommand(
            """
            UPDATE stock
               SET quantity = @quantity, version = version + 1
             WHERE sku = @sku AND version = @expected
            """,
            connection);
        command.Parameters.AddWithValue("quantity", quantity);
        command.Parameters.AddWithValue("sku", sku);
        command.Parameters.AddWithValue("expected", expectedVersion);

        return await command.ExecuteNonQueryAsync();
    }
}
