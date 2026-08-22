using Shouldly;
using Training.Module03.Core;

namespace Training.Module03.Tests.Core;

public sealed class AsyncRetryPolicyTests
{
    [Fact]
    public async Task A_call_that_succeeds_first_time_is_made_once()
    {
        var attempts = 0;

        var result = await AsyncRetryPolicy.ExecuteAsync(
            _ =>
            {
                attempts++;
                return Task.FromResult("ok");
            },
            maxAttempts: 3,
            CancellationToken.None);

        result.ShouldBe("ok");
        attempts.ShouldBe(1);
    }

    [Fact]
    public async Task A_transient_failure_is_retried_until_it_succeeds()
    {
        var attempts = 0;

        var result = await AsyncRetryPolicy.ExecuteAsync(
            _ =>
            {
                attempts++;
                return attempts < 3
                    ? Task.FromException<string>(new HttpRequestException("upstream hiccup"))
                    : Task.FromResult("ok");
            },
            maxAttempts: 5,
            CancellationToken.None);

        result.ShouldBe("ok");
        attempts.ShouldBe(3);
    }

    [Fact]
    public async Task It_gives_up_after_the_attempt_limit()
    {
        var attempts = 0;

        await Should.ThrowAsync<HttpRequestException>(
            async () => await AsyncRetryPolicy.ExecuteAsync(
                _ =>
                {
                    attempts++;
                    return Task.FromException<string>(new HttpRequestException("still down"));
                },
                maxAttempts: 3,
                CancellationToken.None));

        attempts.ShouldBe(3);
    }

    [Fact]
    public async Task Cancellation_stops_it_retrying()
    {
        // A retry loop that ignores its token keeps hammering a dependency that
        // is already known to be failing, long after the caller gave up.
        using var cts = new CancellationTokenSource();
        var attempts = 0;

        await Should.ThrowAsync<OperationCanceledException>(
            async () => await AsyncRetryPolicy.ExecuteAsync<string>(
                async _ =>
                {
                    attempts++;
                    await cts.CancelAsync();
                    throw new HttpRequestException("down");
                },
                maxAttempts: 10,
                cts.Token));

        attempts.ShouldBe(1);
    }

    [Fact]
    public async Task An_already_cancelled_token_prevents_the_first_attempt()
    {
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();
        var attempts = 0;

        await Should.ThrowAsync<OperationCanceledException>(
            async () => await AsyncRetryPolicy.ExecuteAsync(
                _ =>
                {
                    attempts++;
                    return Task.FromResult("ok");
                },
                maxAttempts: 3,
                cts.Token));

        attempts.ShouldBe(0);
    }

    [Fact]
    public async Task The_attempt_number_is_handed_to_the_caller()
    {
        var seen = new List<int>();

        await Should.ThrowAsync<HttpRequestException>(
            async () => await AsyncRetryPolicy.ExecuteAsync(
                attempt =>
                {
                    seen.Add(attempt);
                    return Task.FromException<string>(new HttpRequestException("down"));
                },
                maxAttempts: 3,
                CancellationToken.None));

        seen.ShouldBe([1, 2, 3]);
    }
}
