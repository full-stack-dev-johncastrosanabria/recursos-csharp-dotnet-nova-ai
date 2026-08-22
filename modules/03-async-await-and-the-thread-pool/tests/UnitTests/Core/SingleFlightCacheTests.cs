using Shouldly;
using Training.Module03.Core;

namespace Training.Module03.Tests.Core;

public sealed class SingleFlightCacheTests
{
    [Fact]
    public async Task Returns_the_value_the_factory_produced()
    {
        var cache = new SingleFlightCache<string, int>();

        var value = await cache.GetAsync("a", _ => Task.FromResult(42));

        value.ShouldBe(42);
    }

    [Fact]
    public async Task A_second_call_for_the_same_key_reuses_the_first_result()
    {
        var cache = new SingleFlightCache<string, int>();
        var calls = 0;

        await cache.GetAsync("a", _ => { calls++; return Task.FromResult(1); });
        await cache.GetAsync("a", _ => { calls++; return Task.FromResult(2); });

        calls.ShouldBe(1);
    }

    [Fact]
    public async Task Concurrent_callers_for_one_key_share_a_single_call()
    {
        // This is the stampede. Twenty requests arrive for a key that is not
        // cached; without this, twenty identical queries reach the database.
        var cache = new SingleFlightCache<string, int>();
        var calls = 0;
        var gate = new TaskCompletionSource();

        async Task<int> Slow(string key)
        {
            Interlocked.Increment(ref calls);
            await gate.Task;
            return 7;
        }

        var callers = Enumerable.Range(0, 20).Select(_ => cache.GetAsync("hot", Slow)).ToArray();
        gate.SetResult();
        var results = await Task.WhenAll(callers);

        calls.ShouldBe(1);
        results.ShouldAllBe(r => r == 7);
    }

    [Fact]
    public async Task Different_keys_do_not_share_a_call()
    {
        var cache = new SingleFlightCache<string, int>();
        var calls = 0;

        await cache.GetAsync("a", _ => { calls++; return Task.FromResult(1); });
        await cache.GetAsync("b", _ => { calls++; return Task.FromResult(2); });

        calls.ShouldBe(2);
    }

    [Fact]
    public async Task A_failed_call_is_not_cached()
    {
        // Caching the failure turns one upstream blip into a permanently broken
        // key. The next caller must be allowed to try again.
        var cache = new SingleFlightCache<string, int>();
        var calls = 0;

        await Should.ThrowAsync<InvalidOperationException>(
            async () => await cache.GetAsync("a", _ =>
            {
                calls++;
                return Task.FromException<int>(new InvalidOperationException("upstream down"));
            }));

        var recovered = await cache.GetAsync("a", _ => { calls++; return Task.FromResult(5); });

        calls.ShouldBe(2);
        recovered.ShouldBe(5);
    }

    [Fact]
    public async Task Every_concurrent_caller_sees_the_same_failure()
    {
        var cache = new SingleFlightCache<string, int>();
        var gate = new TaskCompletionSource();

        async Task<int> Failing(string key)
        {
            await gate.Task;
            throw new InvalidOperationException("upstream down");
        }

        var callers = Enumerable.Range(0, 5).Select(_ => cache.GetAsync("hot", Failing)).ToArray();
        gate.SetResult();

        foreach (var caller in callers)
        {
            await Should.ThrowAsync<InvalidOperationException>(async () => await caller);
        }
    }
}
