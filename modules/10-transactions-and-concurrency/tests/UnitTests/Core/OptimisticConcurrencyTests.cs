using Shouldly;
using Training.Module10.Core;

namespace Training.Module10.Tests.Core;

public sealed class OptimisticConcurrencyTests
{
    [Fact]
    public async Task An_uncontended_reservation_succeeds()
    {
        var store = new FakeStockStore(new StockRow("WIDGET-1", 10, 3));

        (await OptimisticConcurrency.TryReserveAsync(store, "WIDGET-1"))
            .ShouldBe(ReserveResult.Reserved);

        store.LastWrite.ShouldBe((9, 3));
    }

    [Fact]
    public async Task A_row_that_moved_underneath_you_is_detected_not_overwritten()
    {
        // The point of the version. Somebody committed between the read and the
        // write, so the write matches nothing.
        var store = new FakeStockStore(new StockRow("WIDGET-1", 10, 3)) { VersionMoves = true };

        (await OptimisticConcurrency.TryReserveAsync(store, "WIDGET-1"))
            .ShouldBe(ReserveResult.LostTheRace);
    }

    [Fact]
    public async Task Losing_the_race_raises_nothing_and_writes_nothing()
    {
        // Zero rows affected is silent. Code that does not check the count has
        // an optimistic scheme that detects nothing at all.
        var store = new FakeStockStore(new StockRow("WIDGET-1", 10, 3)) { VersionMoves = true };

        await Should.NotThrowAsync(() => OptimisticConcurrency.TryReserveAsync(store, "WIDGET-1"));
        store.Committed.ShouldBeFalse();
    }

    [Fact]
    public async Task An_empty_shelf_is_not_a_race()
    {
        var store = new FakeStockStore(new StockRow("WIDGET-1", 0, 7));

        (await OptimisticConcurrency.TryReserveAsync(store, "WIDGET-1"))
            .ShouldBe(ReserveResult.OutOfStock);
    }

    [Fact]
    public async Task And_neither_is_a_missing_sku()
    {
        var store = new FakeStockStore(row: null);

        (await OptimisticConcurrency.TryReserveAsync(store, "NOPE"))
            .ShouldBe(ReserveResult.NoSuchSku);
    }

    [Fact]
    public async Task The_write_carries_the_version_that_was_read()
    {
        var store = new FakeStockStore(new StockRow("WIDGET-1", 4, 11));

        await OptimisticConcurrency.TryReserveAsync(store, "WIDGET-1");

        store.LastWrite.ShouldBe((3, 11));
    }

    private sealed class FakeStockStore(StockRow? row) : IStockStore
    {
        public bool VersionMoves { get; init; }

        public bool Committed { get; private set; }

        public (int Quantity, int ExpectedVersion)? LastWrite { get; private set; }

        public Task<StockRow?> ReadAsync(string sku) => Task.FromResult(row);

        public Task<int> UpdateIfVersionMatchesAsync(string sku, int quantity, int expectedVersion)
        {
            LastWrite = (quantity, expectedVersion);

            if (VersionMoves)
            {
                return Task.FromResult(0);
            }

            Committed = true;

            return Task.FromResult(1);
        }
    }
}
