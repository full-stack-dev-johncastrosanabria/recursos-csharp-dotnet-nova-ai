using Shouldly;
using Training.Module10.Challenge;
using Training.Module10.Core;

namespace Training.Module10.Tests.Challenge;

public sealed class ReservationServiceTests
{
    [Fact]
    public async Task An_uncontended_reservation_takes_one_attempt()
    {
        var store = new RacyStore(quantity: 5, losesFirst: 0);

        (await ReservationService.ReserveAsync(store, "WIDGET-1", NoDelay())).ShouldBe(ReservationOutcome.Reserved);

        store.Attempts.ShouldBe(1);
    }

    [Fact]
    public async Task Losing_the_race_twice_and_then_winning_is_a_success()
    {
        // What the customer sees is a successful order. What the database saw
        // was three attempts.
        var store = new RacyStore(quantity: 5, losesFirst: 2);

        (await ReservationService.ReserveAsync(store, "WIDGET-1", NoDelay())).ShouldBe(ReservationOutcome.Reserved);

        store.Attempts.ShouldBe(3);
    }

    [Fact]
    public async Task Losing_every_time_gives_up_rather_than_looping_forever()
    {
        var store = new RacyStore(quantity: 5, losesFirst: int.MaxValue);

        (await ReservationService.ReserveAsync(store, "WIDGET-1", NoDelay())).ShouldBe(ReservationOutcome.GaveUp);

        store.Attempts.ShouldBe(RetryPolicy.MaxAttempts);
    }

    [Fact]
    public async Task An_empty_shelf_is_answered_immediately()
    {
        // Retrying an empty shelf turns a fast no into a slow one.
        var store = new RacyStore(quantity: 0, losesFirst: 0);

        (await ReservationService.ReserveAsync(store, "WIDGET-1", NoDelay())).ShouldBe(ReservationOutcome.OutOfStock);

        store.Attempts.ShouldBe(1);
    }

    [Fact]
    public async Task An_unknown_sku_is_answered_immediately_too()
    {
        var store = new RacyStore(quantity: 5, losesFirst: 0) { Missing = true };

        (await ReservationService.ReserveAsync(store, "NOPE", NoDelay())).ShouldBe(ReservationOutcome.NoSuchSku);

        store.Attempts.ShouldBe(1);
    }

    [Fact]
    public async Task It_backs_off_between_attempts_and_not_after_the_last()
    {
        var store = new RacyStore(quantity: 5, losesFirst: int.MaxValue);
        var delays = new List<TimeSpan>();

        await ReservationService.ReserveAsync(store, "WIDGET-1", waited =>
        {
            delays.Add(waited);

            return Task.CompletedTask;
        });

        delays.Count.ShouldBe(RetryPolicy.MaxAttempts - 1);
    }

    [Fact]
    public async Task And_does_not_back_off_at_all_when_it_wins_first_time()
    {
        var store = new RacyStore(quantity: 5, losesFirst: 0);
        var delays = 0;

        await ReservationService.ReserveAsync(store, "WIDGET-1", _ => { delays++; return Task.CompletedTask; });

        delays.ShouldBe(0);
    }

    private static Func<TimeSpan, Task> NoDelay() => _ => Task.CompletedTask;

    /// <summary>A store that loses the version race a fixed number of times, then wins.</summary>
    private sealed class RacyStore(int quantity, int losesFirst) : IStockStore
    {
        private int _version;
        private int _writes;

        public bool Missing { get; init; }

        /// <summary>Times round the loop, counted on the read that starts each one.</summary>
        public int Attempts { get; private set; }

        public Task<StockRow?> ReadAsync(string sku)
        {
            Attempts++;

            return Task.FromResult(Missing ? null : new StockRow(sku, quantity, _version));
        }

        public Task<int> UpdateIfVersionMatchesAsync(string sku, int newQuantity, int expectedVersion)
        {
            _writes++;

            if (_writes <= losesFirst)
            {
                _version++;   // somebody else committed between our read and this write

                return Task.FromResult(0);
            }

            return Task.FromResult(1);
        }
    }
}
