namespace Training.Module08.Challenge;

/// <summary>
/// Challenge: retry a failed call without retrying the calls that must not be
/// retried, and without falling into the trap the framework sets for you.
///
/// The trap: an HttpRequestMessage can only be sent once. Send the same
/// instance a second time and you get InvalidOperationException, "The request
/// message was already sent." So a retry is not a loop around base.SendAsync --
/// each attempt needs a fresh message carrying the same method, URI and
/// headers.
///
/// The rules:
///
///   Retry only GET and HEAD. They are safe: repeating one cannot change
///   anything at the far end. A POST that timed out may well have been applied
///   already, and retrying it is how one order becomes two.
///   Retry only on 5xx and 408. A 400 or a 404 will say the same thing however
///   many times you ask.
///   Make at most MaxAttempts attempts in total, then return the last response
///   rather than throwing -- the caller decides what a failure means.
///   Dispose each response you discard.
///
/// Attempts reports how many sends were made, so the tests can see the
/// difference between a retry and a retry that quietly did nothing.
/// </summary>
public sealed class RetryHandler : DelegatingHandler
{
    public RetryHandler(int maxAttempts)
    {
    }

    public int MaxAttempts => throw new NotImplementedException();

    public int Attempts => throw new NotImplementedException();

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
        => throw new NotImplementedException();
}
