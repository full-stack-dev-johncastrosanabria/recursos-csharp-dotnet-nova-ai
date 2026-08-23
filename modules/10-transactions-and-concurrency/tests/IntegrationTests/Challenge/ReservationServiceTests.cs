using System.Data;
using Npgsql;
using Shouldly;
using Training.Module10.Challenge;

namespace Training.Module10.IntegrationTests.Challenge;

[Collection(SharedStockDatabase.Name)]
[Trait("Category", "Integration")]
public sealed class ReservationServiceTests(StockDatabase database)
{
    private const int Buyers = 10;

    private static CancellationToken Token => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Concurrent_buyers_never_take_more_than_there_is()
    {
        // The invariant that matters. Not "everybody succeeds" -- some may
        // legitimately fail -- but that nothing is sold twice.
        await database.ResetAsync("SVC-1", Buyers, Token);

        var outcomes = await RaceAsync("SVC-1", Buyers);

        var reserved = outcomes.Count(outcome => outcome == ReservationOutcome.Reserved);
        var left = await database.QuantityAsync("SVC-1", Token);

        (reserved + left).ShouldBe(Buyers);
        outcomes.ShouldNotContain(ReservationOutcome.NoSuchSku);
    }

    [Fact]
    public async Task But_optimistic_concurrency_starves_on_a_row_this_hot()
    {
        // Worth meeting deliberately: correctness is not the whole story. Ten
        // buyers contending for ONE row means most of them lose the version
        // race repeatedly and exhaust their attempts. Nothing is oversold and
        // most customers are turned away, which is a different kind of wrong.
        await database.ResetAsync("HOT-1", Buyers, Token);

        var outcomes = await RaceAsync("HOT-1", Buyers);

        outcomes.ShouldContain(ReservationOutcome.GaveUp);

        // The right tool for a hot row does not read-then-write at all: it
        // lets the database do the arithmetic in one statement, so there is no
        // window to lose and nothing to retry.
        await database.ResetAsync("HOT-2", Buyers, Token);
        var served = await RaceAtomicDecrementAsync("HOT-2", Buyers);

        served.ShouldBe(Buyers);
        (await database.QuantityAsync("HOT-2", Token)).ShouldBe(0);
    }

    [Fact]
    public async Task Whereas_read_modify_write_under_read_committed_sells_stock_that_is_not_there()
    {
        // The module's real-world case, raced for real. Every buyer reads the
        // same quantity, subtracts one, and writes it back over the others.
        await database.ResetAsync("RMW-1", Buyers, Token);

        var sold = await RaceReadModifyWriteAsync("RMW-1", Buyers);
        var left = await database.QuantityAsync("RMW-1", Token);

        sold.ShouldBe(Buyers);
        left.ShouldBeGreaterThan(0);
        (sold + left).ShouldBeGreaterThan(Buyers);   // units that do not exist

        // The same race, through the version-checked service, conserves stock.
        await database.ResetAsync("RMW-2", Buyers, Token);
        var outcomes = await RaceAsync("RMW-2", Buyers);
        var reserved = outcomes.Count(outcome => outcome == ReservationOutcome.Reserved);

        (reserved + await database.QuantityAsync("RMW-2", Token)).ShouldBe(Buyers);
    }

    [Fact]
    public async Task Nobody_gets_more_than_the_shelf_held()
    {
        await database.ResetAsync("SVC-2", 4, Token);

        var outcomes = await RaceAsync("SVC-2", Buyers);

        outcomes.Count(outcome => outcome == ReservationOutcome.Reserved).ShouldBeLessThanOrEqualTo(4);
        (outcomes.Count(outcome => outcome == ReservationOutcome.Reserved)
            + await database.QuantityAsync("SVC-2", Token)).ShouldBe(4);
    }

    [Fact]
    public async Task An_empty_shelf_is_reported_as_out_of_stock()
    {
        await database.ResetAsync("SVC-4", 0, Token);
        var store = new PostgresStockStore(database.ConnectionString);

        (await ReservationService.ReserveAsync(store, "SVC-4", NoDelay()))
            .ShouldBe(ReservationOutcome.OutOfStock);
    }

    [Fact]
    public async Task Nothing_is_ever_sold_twice()
    {
        // The invariant that matters, stated directly: reservations plus
        // remaining stock equals what was on the shelf.
        await database.ResetAsync("SVC-3", 6, Token);

        var outcomes = await RaceAsync("SVC-3", Buyers);
        var reserved = outcomes.Count(outcome => outcome == ReservationOutcome.Reserved);

        (reserved + await database.QuantityAsync("SVC-3", Token)).ShouldBe(6);
    }

    [Fact]
    public async Task An_unknown_sku_is_reported_rather_than_retried()
    {
        var store = new PostgresStockStore(database.ConnectionString);

        (await ReservationService.ReserveAsync(store, "NOT-A-SKU", NoDelay()))
            .ShouldBe(ReservationOutcome.NoSuchSku);
    }

    private async Task<IReadOnlyList<ReservationOutcome>> RaceAsync(string sku, int buyers)
    {
        var store = new PostgresStockStore(database.ConnectionString);
        var gate = new TaskCompletionSource();

        var races = Enumerable.Range(0, buyers).Select(async _ =>
        {
            await gate.Task;

            return await ReservationService.ReserveAsync(store, sku, NoDelay());
        }).ToArray();

        gate.SetResult();

        return await Task.WhenAll(races);
    }

    /// <summary>The naive path: read, think, write back. No version, no lock.</summary>
    private async Task<int> RaceReadModifyWriteAsync(string sku, int buyers)
    {
        var sold = 0;
        var gate = new TaskCompletionSource();

        var races = Enumerable.Range(0, buyers).Select(async _ =>
        {
            await gate.Task;

            await using var connection = new NpgsqlConnection(database.ConnectionString);
            await connection.OpenAsync(Token);
            await using var transaction =
                await connection.BeginTransactionAsync(IsolationLevel.ReadCommitted, Token);

            int quantity;
            await using (var read = new NpgsqlCommand(
                "SELECT quantity FROM stock WHERE sku = @sku", connection, transaction))
            {
                read.Parameters.AddWithValue("sku", sku);
                quantity = (int)(await read.ExecuteScalarAsync(Token))!;
            }

            await Task.Delay(40, Token);   // the window every real handler has

            if (quantity <= 0)
            {
                await transaction.RollbackAsync(Token);

                return;
            }

            await using (var write = new NpgsqlCommand(
                "UPDATE stock SET quantity = @quantity WHERE sku = @sku", connection, transaction))
            {
                write.Parameters.AddWithValue("quantity", quantity - 1);
                write.Parameters.AddWithValue("sku", sku);
                await write.ExecuteNonQueryAsync(Token);
            }

            await transaction.CommitAsync(Token);
            Interlocked.Increment(ref sold);
        }).ToArray();

        gate.SetResult();
        await Task.WhenAll(races);

        return sold;
    }

    /// <summary>One statement, no window: UPDATE ... SET quantity = quantity - 1.</summary>
    private async Task<int> RaceAtomicDecrementAsync(string sku, int buyers)
    {
        var served = 0;
        var gate = new TaskCompletionSource();

        var races = Enumerable.Range(0, buyers).Select(async _ =>
        {
            await gate.Task;

            await using var connection = new NpgsqlConnection(database.ConnectionString);
            await connection.OpenAsync(Token);
            await using var command = new NpgsqlCommand(
                """
                UPDATE stock SET quantity = quantity - 1
                 WHERE sku = @sku AND quantity > 0
                """,
                connection);
            command.Parameters.AddWithValue("sku", sku);

            if (await command.ExecuteNonQueryAsync(Token) == 1)
            {
                Interlocked.Increment(ref served);
            }
        }).ToArray();

        gate.SetResult();
        await Task.WhenAll(races);

        return served;
    }

    private static Func<TimeSpan, Task> NoDelay() => _ => Task.CompletedTask;
}
