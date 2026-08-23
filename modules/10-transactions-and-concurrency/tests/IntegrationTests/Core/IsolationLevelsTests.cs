using System.Data;
using Npgsql;
using Shouldly;
using Training.Module10.Core;

namespace Training.Module10.IntegrationTests.Core;

[Collection(SharedStockDatabase.Name)]
[Trait("Category", "Integration")]
public sealed class IsolationLevelsTests(StockDatabase database)
{
    private static CancellationToken Token => TestContext.Current.CancellationToken;

    [Fact]
    public async Task PostgreSQL_accepts_read_uncommitted_and_even_says_it_is_using_it()
    {
        // Which is the trap. You cannot detect this by asking the server what
        // level it is running at -- it echoes back exactly what you requested.
        (await database.ReportedIsolationAsync(IsolationLevel.ReadUncommitted, Token))
            .ShouldBe("read uncommitted");

        // So the label the server reports and the behaviour it delivers
        // disagree, and only one of them is worth acting on.
        IsolationLevels.EffectiveInPostgres(IsolationLevel.ReadUncommitted)
            .ShouldNotBe(IsolationLevel.ReadUncommitted);
    }

    [Fact]
    public async Task But_a_dirty_read_still_does_not_happen()
    {
        // Only behaviour reveals it. PostgreSQL isolates with snapshots, and a
        // snapshot cannot contain uncommitted data, so the level is accepted
        // and then implemented as READ COMMITTED.
        await database.ResetAsync("DIRTY-1", 10, Token);

        await using var reader = new NpgsqlConnection(database.ConnectionString);
        await reader.OpenAsync(Token);
        await using var readerTransaction =
            await reader.BeginTransactionAsync(IsolationLevel.ReadUncommitted, Token);

        await using var writer = new NpgsqlConnection(database.ConnectionString);
        await writer.OpenAsync(Token);
        await using var writerTransaction = await writer.BeginTransactionAsync(Token);

        await using (var write = new NpgsqlCommand(
            "UPDATE stock SET quantity = 999 WHERE sku='DIRTY-1'", writer, writerTransaction))
        {
            await write.ExecuteNonQueryAsync(Token);
        }

        // The writer has not committed. A level that genuinely permitted dirty
        // reads would show 999 here.
        await using var read = new NpgsqlCommand(
            "SELECT quantity FROM stock WHERE sku='DIRTY-1'", reader, readerTransaction);
        var seen = (int)(await read.ExecuteScalarAsync(Token))!;

        seen.ShouldBe(10);
        await writerTransaction.RollbackAsync(Token);

        IsolationLevels.EffectiveInPostgres(IsolationLevel.ReadUncommitted)
            .ShouldBe(IsolationLevel.ReadCommitted);
    }

    [Theory]
    [InlineData(IsolationLevel.ReadCommitted, "read committed")]
    [InlineData(IsolationLevel.RepeatableRead, "repeatable read")]
    [InlineData(IsolationLevel.Serializable, "serializable")]
    public async Task Every_other_level_is_honoured_as_asked(IsolationLevel requested, string expected)
    {
        (await database.ReportedIsolationAsync(requested, Token)).ShouldBe(expected);
        IsolationLevels.EffectiveInPostgres(requested).ShouldBe(requested);
    }
}
