namespace Training.Module03.Challenge;

/// <summary>
/// Turns a callback-style API into something awaitable.
///
/// Challenge: back this with TaskCompletionSource&lt;T&gt;. Two details carry the
/// exercise. Callback APIs fire twice more often than their documentation
/// admits — a timeout races a response, a retry arrives late — so a second
/// callback must be a non-event rather than an exception on a thread nobody is
/// catching. And by default a continuation runs inline on whichever thread
/// completed the task, which puts the awaiter's work on the device callback's
/// thread and blocks it; the fix is one constructor argument.
/// </summary>
public sealed class CallbackBridge<T>
{
    public Task<T> Task => throw new NotImplementedException();

    public void Complete(T value) => throw new NotImplementedException();

    public void Fail(Exception error) => throw new NotImplementedException();

    public void Cancel() => throw new NotImplementedException();
}
