using Shouldly;
using Training.Module03.Challenge;

namespace Training.Module03.Tests.Challenge;

public sealed class CallbackBridgeTests
{
    [Fact]
    public async Task Completing_produces_the_value()
    {
        var bridge = new CallbackBridge<int>();

        bridge.Complete(42);

        (await bridge.Task).ShouldBe(42);
    }

    [Fact]
    public void The_task_does_not_finish_before_the_callback_arrives()
    {
        var bridge = new CallbackBridge<int>();

        bridge.Task.IsCompleted.ShouldBeFalse();
    }

    [Fact]
    public async Task Failing_surfaces_the_exception_to_the_awaiter()
    {
        var bridge = new CallbackBridge<int>();

        bridge.Fail(new InvalidOperationException("the device rejected it"));

        var error = await Should.ThrowAsync<InvalidOperationException>(async () => await bridge.Task);
        error.Message.ShouldBe("the device rejected it");
    }

    [Fact]
    public async Task Cancelling_surfaces_as_a_cancellation()
    {
        var bridge = new CallbackBridge<int>();

        bridge.Cancel();

        await Should.ThrowAsync<OperationCanceledException>(async () => await bridge.Task);
    }

    [Fact]
    public async Task A_second_callback_is_ignored_rather_than_throwing()
    {
        // Callback APIs fire twice more often than their documentation admits:
        // a timeout races a response, a retry arrives late. Using the Set*
        // methods here throws on a background thread nobody is catching, which
        // takes the process down; the Try* methods make it a non-event.
        var bridge = new CallbackBridge<int>();

        bridge.Complete(1);
        Should.NotThrow(() => bridge.Complete(2));
        Should.NotThrow(() => bridge.Fail(new InvalidOperationException("late failure")));
        Should.NotThrow(bridge.Cancel);

        (await bridge.Task).ShouldBe(1);
    }

    [Fact]
    public async Task Completing_does_not_run_the_awaiter_on_the_completing_thread()
    {
        // The subtle one. By default a continuation runs inline on whichever
        // thread completed the task -- so an awaiter's work lands on the
        // device callback's thread, or the socket's IO thread, and blocks it.
        // RunContinuationsAsynchronously is the fix, and it is one argument.
        var bridge = new CallbackBridge<int>();
        var completingThread = 0;
        var continuationThread = 0;

        var continuation = bridge.Task.ContinueWith(
            _ => continuationThread = Environment.CurrentManagedThreadId,
            TaskScheduler.Default);

        var completer = new Thread(() =>
        {
            completingThread = Environment.CurrentManagedThreadId;
            bridge.Complete(1);
        });

        completer.Start();
        completer.Join();

        await continuation;

        completingThread.ShouldNotBe(0);
        continuationThread.ShouldNotBe(completingThread);
    }
}
