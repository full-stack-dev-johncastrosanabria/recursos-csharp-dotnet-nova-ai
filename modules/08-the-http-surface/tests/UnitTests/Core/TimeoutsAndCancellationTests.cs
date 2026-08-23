using System.Net;
using Shouldly;
using Training.Module08.Core;

namespace Training.Module08.Tests.Core;

public sealed class TimeoutsAndCancellationTests
{
    private static CancellationToken Token => TestContext.Current.CancellationToken;

    [Fact]
    public async Task A_prompt_response_is_simply_completed()
    {
        using var primary = new StubPrimaryHandler();
        using var client = new HttpClient(primary) { BaseAddress = new Uri("https://gateway.invalid/") };

        var outcome = await TimeoutsAndCancellation.ClassifyAsync(client, "orders", Token);

        outcome.ShouldBe(TimeoutsAndCancellation.Completed);
    }

    [Fact]
    public async Task An_elapsed_client_timeout_is_a_timeout()
    {
        using var primary = Slow();
        using var client = new HttpClient(primary)
        {
            BaseAddress = new Uri("https://gateway.invalid/"),
            Timeout = TimeSpan.FromMilliseconds(120),
        };

        var outcome = await TimeoutsAndCancellation.ClassifyAsync(client, "orders", Token);

        outcome.ShouldBe(TimeoutsAndCancellation.Timeout);
    }

    [Fact]
    public async Task A_cancelled_caller_token_is_a_cancellation()
    {
        using var primary = Slow();
        using var client = new HttpClient(primary) { BaseAddress = new Uri("https://gateway.invalid/") };
        using var caller = new CancellationTokenSource(TimeSpan.FromMilliseconds(120));

        var outcome = await TimeoutsAndCancellation.ClassifyAsync(client, "orders", caller.Token);

        outcome.ShouldBe(TimeoutsAndCancellation.Cancelled);
    }

    [Fact]
    public async Task An_already_cancelled_caller_wins_over_a_short_timeout()
    {
        // Both mechanisms are armed. The caller went away first, so this is a
        // cancellation -- and reporting it as a dependency timeout would put
        // the blame on a service that was never even called.
        using var primary = Slow();
        using var client = new HttpClient(primary)
        {
            BaseAddress = new Uri("https://gateway.invalid/"),
            Timeout = TimeSpan.FromMilliseconds(120),
        };
        using var caller = new CancellationTokenSource();
        await caller.CancelAsync();

        var outcome = await TimeoutsAndCancellation.ClassifyAsync(client, "orders", caller.Token);

        outcome.ShouldBe(TimeoutsAndCancellation.Cancelled);
    }

    private static StubPrimaryHandler Slow()
        => new(async (_, cancellationToken) =>
        {
            await Task.Delay(TimeSpan.FromSeconds(30), cancellationToken);

            return new HttpResponseMessage(HttpStatusCode.OK);
        });
}
