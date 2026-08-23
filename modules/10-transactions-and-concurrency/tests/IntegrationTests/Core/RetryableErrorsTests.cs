using System.Data;
using Npgsql;
using Shouldly;
using Training.Module10.Core;

namespace Training.Module10.IntegrationTests.Core;

[Collection(SharedStockDatabase.Name)]
[Trait("Category", "Integration")]
public sealed class RetryableErrorsTests(StockDatabase database)
{
    private static CancellationToken Token => TestContext.Current.CancellationToken;

    [Fact]
    public async Task A_duplicate_key_really_is_23505_and_really_is_a_conflict()
    {
        await database.ResetAsync("DUP-1", 1, Token);

        var sqlState = await StockDatabase.SqlStateOfAsync(() =>
            database.ExecuteAsync("INSERT INTO stock (sku, quantity) VALUES ('DUP-1', 1)", Token));

        sqlState.ShouldBe("23505");
        RetryableErrors.Classify(sqlState!).ShouldBe(FailureKind.Conflict);
    }

    [Fact]
    public async Task A_serialization_failure_really_is_40001_and_really_is_retryable()
    {
        // Two REPEATABLE READ transactions that both read the row and both try
        // to write it. One of them is told to start again.
        await database.ResetAsync("SER-1", 10, Token);

        string? sqlState = null;
        var blocked = Task.Run(async () =>
        {
            await using var connection = new NpgsqlConnection(database.ConnectionString);
            await connection.OpenAsync(Token);
            await using var transaction =
                await connection.BeginTransactionAsync(IsolationLevel.RepeatableRead, Token);

            await using (var read = new NpgsqlCommand(
                "SELECT quantity FROM stock WHERE sku='SER-1'", connection, transaction))
            {
                await read.ExecuteScalarAsync(Token);
            }

            await Task.Delay(200, Token);

            sqlState = await StockDatabase.SqlStateOfAsync(async () =>
            {
                await using var write = new NpgsqlCommand(
                    "UPDATE stock SET quantity = 1 WHERE sku='SER-1'", connection, transaction);
                await write.ExecuteNonQueryAsync(Token);
                await transaction.CommitAsync(Token);
            });
        }, Token);

        await Task.Delay(60, Token);
        await database.ExecuteAsync("UPDATE stock SET quantity = 2 WHERE sku='SER-1'", Token);
        await blocked;

        sqlState.ShouldBe("40001");
        RetryableErrors.Classify(sqlState!).ShouldBe(FailureKind.Retryable);
    }

    [Fact]
    public async Task A_deadlock_really_is_40P01_and_really_is_retryable()
    {
        // Two transactions taking the same two rows in opposite order -- the
        // cycle exercise 5 exists to prevent.
        await database.ResetAsync("DL-1", 10, Token);
        await database.ResetAsync("DL-2", 10, Token);

        string? sqlState = null;

        async Task Take(string first, string second)
        {
            var observed = await StockDatabase.SqlStateOfAsync(async () =>
            {
                await using var connection = new NpgsqlConnection(database.ConnectionString);
                await connection.OpenAsync(Token);
                await using var transaction = await connection.BeginTransactionAsync(Token);

                await Update(connection, transaction, first);
                await Task.Delay(250, Token);
                await Update(connection, transaction, second);

                await transaction.CommitAsync(Token);
            });

            if (observed is not null)
            {
                sqlState = observed;
            }
        }

        await Task.WhenAll(Take("DL-1", "DL-2"), Take("DL-2", "DL-1"));

        sqlState.ShouldBe("40P01");
        RetryableErrors.ShouldRetry(sqlState!).ShouldBeTrue();
    }

    private static async Task Update(NpgsqlConnection connection, NpgsqlTransaction transaction, string sku)
    {
        await using var command = new NpgsqlCommand(
            "UPDATE stock SET quantity = quantity - 1 WHERE sku = @sku", connection, transaction);
        command.Parameters.AddWithValue("sku", sku);
        await command.ExecuteNonQueryAsync(TestContext.Current.CancellationToken);
    }
}
