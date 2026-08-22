using Shouldly;
using Training.Module03.Core;

namespace Training.Module03.Tests.Core;

public sealed class BoundedFanOutTests
{
    private sealed class ConcurrencyProbe
    {
        private int _inFlight;

        public int HighWaterMark { get; private set; }

        public int Calls { get; private set; }

        public async Task<int> InvokeAsync(int input)
        {
            lock (this)
            {
                _inFlight++;
                Calls++;
                HighWaterMark = Math.Max(HighWaterMark, _inFlight);
            }

            await Task.Delay(15);

            lock (this)
            {
                _inFlight--;
            }

            return input * 2;
        }
    }

    [Fact]
    public async Task Every_item_is_processed()
    {
        var probe = new ConcurrencyProbe();

        var results = await BoundedFanOut.RunAsync(
            Enumerable.Range(1, 10).ToArray(), maxConcurrency: 3, probe.InvokeAsync, CancellationToken.None);

        results.Length.ShouldBe(10);
        probe.Calls.ShouldBe(10);
    }

    [Fact]
    public async Task Results_come_back_in_input_order()
    {
        var probe = new ConcurrencyProbe();

        var results = await BoundedFanOut.RunAsync(
            [1, 2, 3, 4, 5], maxConcurrency: 2, probe.InvokeAsync, CancellationToken.None);

        results.ShouldBe([2, 4, 6, 8, 10]);
    }

    [Fact]
    public async Task Never_exceeds_the_concurrency_limit()
    {
        // Unbounded fan-out is the failure this prevents: WhenAll over a
        // thousand items opens a thousand connections at once, and the pool
        // that was protecting the database becomes the thing that exhausts it.
        var probe = new ConcurrencyProbe();

        await BoundedFanOut.RunAsync(
            Enumerable.Range(1, 30).ToArray(), maxConcurrency: 4, probe.InvokeAsync, CancellationToken.None);

        probe.HighWaterMark.ShouldBeLessThanOrEqualTo(4);
    }

    [Fact]
    public async Task It_really_does_run_things_in_parallel()
    {
        var probe = new ConcurrencyProbe();

        await BoundedFanOut.RunAsync(
            Enumerable.Range(1, 20).ToArray(), maxConcurrency: 5, probe.InvokeAsync, CancellationToken.None);

        probe.HighWaterMark.ShouldBeGreaterThan(1);
    }

    [Fact]
    public async Task An_empty_input_returns_an_empty_result()
    {
        var probe = new ConcurrencyProbe();

        var results = await BoundedFanOut.RunAsync<int, int>([], maxConcurrency: 4, probe.InvokeAsync, CancellationToken.None);

        results.ShouldBeEmpty();
        probe.Calls.ShouldBe(0);
    }

    [Fact]
    public async Task A_failure_surfaces_to_the_caller()
    {
        static Task<int> Fail(int input)
            => input == 3
                ? Task.FromException<int>(new InvalidOperationException("item 3 failed"))
                : Task.FromResult(input);

        await Should.ThrowAsync<InvalidOperationException>(
            async () => await BoundedFanOut.RunAsync([1, 2, 3, 4], maxConcurrency: 2, Fail, CancellationToken.None));
    }

    [Fact]
    public async Task Cancellation_is_honoured()
    {
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        await Should.ThrowAsync<OperationCanceledException>(
            async () => await BoundedFanOut.RunAsync(
                [1, 2, 3], maxConcurrency: 2, i => Task.FromResult(i), cts.Token));
    }
}
