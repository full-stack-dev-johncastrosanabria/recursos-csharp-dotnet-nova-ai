using Shouldly;
using Training.Module10.Challenge;

namespace Training.Module10.Tests.Challenge;

public sealed class IdempotentOperationsTests
{
    [Fact]
    public async Task The_first_call_does_the_work()
    {
        var log = new FakeLog();
        var ran = 0;

        var result = await IdempotentOperations.ExecuteOnceAsync(
            log, "order-1", () => { ran++; return Task.FromResult("charged"); });

        result.ShouldBe("charged");
        ran.ShouldBe(1);
    }

    [Fact]
    public async Task The_second_call_with_the_same_key_does_not()
    {
        // The retry is now a lookup, which is what makes it safe.
        var log = new FakeLog();
        var ran = 0;

        await IdempotentOperations.ExecuteOnceAsync(
            log, "order-1", () => { ran++; return Task.FromResult("charged"); });
        var second = await IdempotentOperations.ExecuteOnceAsync(
            log, "order-1", () => { ran++; return Task.FromResult("charged again"); });

        ran.ShouldBe(1);
        second.ShouldBe("charged");
    }

    [Fact]
    public async Task A_different_key_is_a_different_operation()
    {
        var log = new FakeLog();
        var ran = 0;

        await IdempotentOperations.ExecuteOnceAsync(log, "order-1", () => { ran++; return Task.FromResult("a"); });
        await IdempotentOperations.ExecuteOnceAsync(log, "order-2", () => { ran++; return Task.FromResult("b"); });

        ran.ShouldBe(2);
    }

    [Fact]
    public async Task A_failed_attempt_is_not_recorded()
    {
        // Recording a failure would make a transient problem permanent, and
        // every retry would faithfully reproduce it.
        var log = new FakeLog();

        await Should.ThrowAsync<TimeoutException>(() => IdempotentOperations.ExecuteOnceAsync(
            log, "order-1", () => throw new TimeoutException("gateway")));

        log.Records.ShouldBeEmpty();
    }

    [Fact]
    public async Task So_the_operation_can_still_be_retried_afterwards()
    {
        var log = new FakeLog();
        var attempts = 0;

        await Should.ThrowAsync<TimeoutException>(() => IdempotentOperations.ExecuteOnceAsync(
            log, "order-1", () => { attempts++; throw new TimeoutException("gateway"); }));

        var result = await IdempotentOperations.ExecuteOnceAsync(
            log, "order-1", () => { attempts++; return Task.FromResult("charged"); });

        attempts.ShouldBe(2);
        result.ShouldBe("charged");
    }

    private sealed class FakeLog : IOperationLog
    {
        public Dictionary<string, string> Records { get; } = new(StringComparer.Ordinal);

        public Task<string?> ResultForAsync(string key)
            => Task.FromResult(Records.GetValueOrDefault(key));

        public Task RecordAsync(string key, string result)
        {
            Records[key] = result;

            return Task.CompletedTask;
        }
    }
}
