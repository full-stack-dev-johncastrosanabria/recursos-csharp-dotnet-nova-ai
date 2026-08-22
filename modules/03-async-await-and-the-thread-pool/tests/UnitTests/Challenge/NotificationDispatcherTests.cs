using Shouldly;
using Training.Module03.Challenge;

namespace Training.Module03.Tests.Challenge;

public sealed class NotificationDispatcherTests
{
    [Fact]
    public async Task Enqueued_work_is_processed()
    {
        var handled = new List<string>();
        var dispatcher = new NotificationDispatcher((item, _) =>
        {
            lock (handled) { handled.Add(item); }
            return Task.CompletedTask;
        });

        await dispatcher.EnqueueAsync("a");
        await dispatcher.EnqueueAsync("b");
        await dispatcher.DisposeAsync();

        handled.Count.ShouldBe(2);
    }

    [Fact]
    public async Task Disposal_drains_what_is_still_queued()
    {
        // Anything that buffers owes its callers a drain on the way out. A
        // queue that drops its backlog on shutdown loses work every deploy,
        // and it loses it silently -- every EnqueueAsync returned successfully.
        var handled = new List<string>();
        var gate = new TaskCompletionSource();
        var dispatcher = new NotificationDispatcher(async (item, _) =>
        {
            await gate.Task;
            lock (handled) { handled.Add(item); }
        });

        for (var i = 0; i < 25; i++)
        {
            await dispatcher.EnqueueAsync($"item-{i}");
        }

        gate.SetResult();
        await dispatcher.DisposeAsync();

        handled.Count.ShouldBe(25);
    }

    [Fact]
    public async Task Items_are_processed_in_the_order_they_arrived()
    {
        var handled = new List<string>();
        var dispatcher = new NotificationDispatcher((item, _) =>
        {
            handled.Add(item);
            return Task.CompletedTask;
        });

        await dispatcher.EnqueueAsync("first");
        await dispatcher.EnqueueAsync("second");
        await dispatcher.EnqueueAsync("third");
        await dispatcher.DisposeAsync();

        handled.ShouldBe(["first", "second", "third"]);
    }

    [Fact]
    public async Task Enqueueing_after_disposal_throws()
    {
        var dispatcher = new NotificationDispatcher((_, _) => Task.CompletedTask);
        await dispatcher.DisposeAsync();

        await Should.ThrowAsync<ObjectDisposedException>(async () => await dispatcher.EnqueueAsync("late"));
    }

    [Fact]
    public async Task A_handler_failure_is_reported_at_disposal_rather_than_lost()
    {
        // A fire-and-forget task whose exception nobody observes is the classic
        // way to lose an error completely. The queue must hold onto it.
        var dispatcher = new NotificationDispatcher((item, _) => item == "bad"
            ? Task.FromException(new InvalidOperationException("handler failed"))
            : Task.CompletedTask);

        await dispatcher.EnqueueAsync("good");
        await dispatcher.EnqueueAsync("bad");

        await Should.ThrowAsync<InvalidOperationException>(async () => await dispatcher.DisposeAsync());
    }

    [Fact]
    public async Task Disposing_twice_is_harmless()
    {
        var dispatcher = new NotificationDispatcher((_, _) => Task.CompletedTask);

        await dispatcher.EnqueueAsync("a");
        await dispatcher.DisposeAsync();
        await Should.NotThrowAsync(async () => await dispatcher.DisposeAsync());
    }
}
