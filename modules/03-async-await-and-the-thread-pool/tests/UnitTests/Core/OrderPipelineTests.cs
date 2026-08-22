using Shouldly;
using Training.Module03.Core;

namespace Training.Module03.Tests.Core;

public sealed class OrderPipelineTests
{
    /// <summary>
    /// Records how many calls were in flight at once, so concurrency can be
    /// asserted without measuring elapsed time. A timing assertion would be
    /// flaky on a loaded machine; a high-water mark is exact.
    /// </summary>
    private sealed class ConcurrencyProbe
    {
        private int _inFlight;

        public int HighWaterMark { get; private set; }

        public int Calls { get; private set; }

        public async Task<string> InvokeAsync(string input)
        {
            lock (this)
            {
                _inFlight++;
                Calls++;
                HighWaterMark = Math.Max(HighWaterMark, _inFlight);
            }

            await Task.Yield();
            await Task.Delay(20);

            lock (this)
            {
                _inFlight--;
            }

            return input.ToUpperInvariant();
        }
    }

    [Fact]
    public async Task Every_input_produces_a_result_in_order()
    {
        var probe = new ConcurrencyProbe();

        var results = await OrderPipeline.EnrichAllAsync(["a", "b", "c"], probe.InvokeAsync);

        results.ShouldBe(["A", "B", "C"]);
    }

    [Fact]
    public async Task The_calls_actually_overlap()
    {
        // The point of the exercise. Awaiting inside the loop is sequential and
        // still passes the test above; only the high-water mark tells them apart.
        var probe = new ConcurrencyProbe();

        await OrderPipeline.EnrichAllAsync(["a", "b", "c", "d"], probe.InvokeAsync);

        probe.HighWaterMark.ShouldBe(4);
    }

    [Fact]
    public async Task Each_input_is_visited_exactly_once()
    {
        var probe = new ConcurrencyProbe();

        await OrderPipeline.EnrichAllAsync(["a", "b", "c"], probe.InvokeAsync);

        probe.Calls.ShouldBe(3);
    }

    [Fact]
    public async Task An_empty_input_does_no_work()
    {
        var probe = new ConcurrencyProbe();

        var results = await OrderPipeline.EnrichAllAsync([], probe.InvokeAsync);

        results.ShouldBeEmpty();
        probe.Calls.ShouldBe(0);
    }

    [Fact]
    public async Task One_failure_surfaces_rather_than_being_swallowed()
    {
        static Task<string> Fail(string input)
            => input == "b"
                ? Task.FromException<string>(new InvalidOperationException("enrichment failed"))
                : Task.FromResult(input);

        var error = await Should.ThrowAsync<InvalidOperationException>(
            async () => await OrderPipeline.EnrichAllAsync(["a", "b", "c"], Fail));

        error.Message.ShouldBe("enrichment failed");
    }

    [Fact]
    public async Task Every_call_is_started_even_when_an_early_one_fails()
    {
        // WhenAll starts everything before observing any result. A loop that
        // awaits each call in turn stops at the first failure, and the work
        // after it never happens -- a real difference in behaviour, not style.
        var started = 0;

        Task<string> Count(string input)
        {
            Interlocked.Increment(ref started);
            return input == "a"
                ? Task.FromException<string>(new InvalidOperationException("first one fails"))
                : Task.FromResult(input);
        }

        await Should.ThrowAsync<InvalidOperationException>(
            async () => await OrderPipeline.EnrichAllAsync(["a", "b", "c"], Count));

        started.ShouldBe(3);
    }
}
