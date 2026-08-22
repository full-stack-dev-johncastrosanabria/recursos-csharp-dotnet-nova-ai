using Shouldly;
using Training.Module03.Core;

namespace Training.Module03.Tests.Core;

public sealed class CancellationRelayTests
{
    [Fact]
    public async Task Passes_the_token_through_to_the_work()
    {
        using var cts = new CancellationTokenSource();
        CancellationToken seen = default;

        await CancellationRelay.RunAsync(
            token =>
            {
                seen = token;
                return Task.FromResult(1);
            },
            cts.Token);

        seen.ShouldBe(cts.Token);
    }

    [Fact]
    public async Task Returns_the_result_when_nothing_is_cancelled()
    {
        using var cts = new CancellationTokenSource();

        var result = await CancellationRelay.RunAsync(_ => Task.FromResult(42), cts.Token);

        result.ShouldBe(42);
    }

    [Fact]
    public async Task Refuses_to_start_work_that_is_already_cancelled()
    {
        // Checking before starting is not an optimisation. Work begun on a
        // cancelled token still consumes a connection, a thread and a retry
        // budget, and its result is thrown away.
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();
        var started = false;

        await Should.ThrowAsync<OperationCanceledException>(
            async () => await CancellationRelay.RunAsync(
                _ =>
                {
                    started = true;
                    return Task.FromResult(1);
                },
                cts.Token));

        started.ShouldBeFalse();
    }

    [Fact]
    public async Task Cancellation_raised_during_the_work_surfaces()
    {
        using var cts = new CancellationTokenSource();

        await Should.ThrowAsync<OperationCanceledException>(
            async () => await CancellationRelay.RunAsync(
                async token =>
                {
                    await cts.CancelAsync();
                    token.ThrowIfCancellationRequested();
                    return 1;
                },
                cts.Token));
    }

    [Fact]
    public async Task A_real_failure_is_not_reported_as_a_cancellation()
    {
        using var cts = new CancellationTokenSource();

        await Should.ThrowAsync<InvalidOperationException>(
            async () => await CancellationRelay.RunAsync(
                _ => Task.FromException<int>(new InvalidOperationException("the database said no")),
                cts.Token));
    }

    [Fact]
    public async Task None_of_this_needs_a_token_source_to_be_disposed_twice()
    {
        var cts = new CancellationTokenSource();
        var result = await CancellationRelay.RunAsync(_ => Task.FromResult(7), cts.Token);
        cts.Dispose();
        cts.Dispose();

        result.ShouldBe(7);
    }
}
