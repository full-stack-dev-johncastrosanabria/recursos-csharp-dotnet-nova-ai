using Shouldly;
using Training.Module10.Core;

namespace Training.Module10.Tests.Core;

public sealed class RetryPolicyTests
{
    [Fact]
    public async Task An_operation_that_works_first_time_is_not_retried()
    {
        var attempts = 0;

        var outcome = await RetryPolicy.ExecuteAsync(
            () => { attempts++; return Task.CompletedTask; },
            NoDelay());

        outcome.ShouldBe(new RetryOutcome(true, 1, null));
        attempts.ShouldBe(1);
    }

    [Fact]
    public async Task A_serialization_failure_is_retried_until_it_succeeds()
    {
        var attempts = 0;

        var outcome = await RetryPolicy.ExecuteAsync(
            () =>
            {
                attempts++;

                return attempts < 3 ? throw new DatabaseFailureException("40001") : Task.CompletedTask;
            },
            NoDelay());

        outcome.Succeeded.ShouldBeTrue();
        outcome.Attempts.ShouldBe(3);
    }

    [Fact]
    public async Task A_deadlock_is_retried_too()
    {
        var attempts = 0;

        var outcome = await RetryPolicy.ExecuteAsync(
            () =>
            {
                attempts++;

                return attempts < 2 ? throw new DatabaseFailureException("40P01") : Task.CompletedTask;
            },
            NoDelay());

        outcome.Succeeded.ShouldBeTrue();
        outcome.Attempts.ShouldBe(2);
    }

    [Fact]
    public async Task A_duplicate_key_is_not_retried_even_once()
    {
        // Four identical errors is not better than one.
        var attempts = 0;

        var outcome = await RetryPolicy.ExecuteAsync(
            () => { attempts++; throw new DatabaseFailureException("23505"); },
            NoDelay());

        attempts.ShouldBe(1);
        outcome.ShouldBe(new RetryOutcome(false, 1, "23505"));
    }

    [Fact]
    public async Task Persistent_contention_gives_up_at_the_limit()
    {
        var attempts = 0;

        var outcome = await RetryPolicy.ExecuteAsync(
            () => { attempts++; throw new DatabaseFailureException("40001"); },
            NoDelay());

        attempts.ShouldBe(RetryPolicy.MaxAttempts);
        outcome.ShouldBe(new RetryOutcome(false, RetryPolicy.MaxAttempts, "40001"));
    }

    [Fact]
    public async Task The_delay_runs_between_attempts_and_not_after_the_last()
    {
        var delays = new List<TimeSpan>();

        await RetryPolicy.ExecuteAsync(
            () => throw new DatabaseFailureException("40001"),
            waited => { delays.Add(waited); return Task.CompletedTask; });

        delays.Count.ShouldBe(RetryPolicy.MaxAttempts - 1);
    }

    [Fact]
    public void Backoff_doubles_with_each_attempt()
    {
        RetryPolicy.DelayFor(1, 0.5).ShouldBe(RetryPolicy.BaseDelay);
        RetryPolicy.DelayFor(2, 0.5).ShouldBe(RetryPolicy.BaseDelay * 2);
        RetryPolicy.DelayFor(3, 0.5).ShouldBe(RetryPolicy.BaseDelay * 4);
    }

    [Fact]
    public void And_jitter_spreads_it_from_half_to_one_and_a_half()
    {
        // Without this, everything that collided retries at the same instant
        // and collides again.
        RetryPolicy.DelayFor(1, 0).ShouldBe(RetryPolicy.BaseDelay * 0.5);
        RetryPolicy.DelayFor(1, 1).ShouldBe(RetryPolicy.BaseDelay * 1.5);
    }

    private static Func<TimeSpan, Task> NoDelay() => _ => Task.CompletedTask;
}
