namespace Training.Module03.Challenge;

/// <summary>
/// Turns a callback-style API into something awaitable.
///
/// Two decisions, both one token long, both the difference between working and
/// working-until-production.
///
/// RunContinuationsAsynchronously: without it, whoever completes the task also
/// runs every awaiter's continuation, inline, on their thread. That thread is
/// usually not yours -- a device callback, a socket IO thread, a driver's
/// notification thread -- and the awaiter's work now blocks it.
///
/// TrySetResult rather than SetResult: callback APIs fire twice more often than
/// their documentation admits, and SetResult throws on the second one. That
/// exception is raised on the callback's thread, where nobody is catching it.
/// </summary>
public sealed class CallbackBridge<T>
{
    private readonly TaskCompletionSource<T> _source =
        new(TaskCreationOptions.RunContinuationsAsynchronously);

    public Task<T> Task => _source.Task;

    public void Complete(T value) => _source.TrySetResult(value);

    public void Fail(Exception error) => _source.TrySetException(error);

    public void Cancel() => _source.TrySetCanceled();
}
