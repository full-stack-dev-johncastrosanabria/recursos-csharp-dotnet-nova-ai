namespace Training.Module08.Core;

/// <summary>
/// Exercise: tell a timeout apart from a cancellation.
///
/// Both surface as TaskCanceledException, which is why so much code treats them
/// as the same event and logs "operation cancelled" for an outage. They are not
/// the same event: a timeout means the dependency is too slow and is a fact
/// about the dependency; a cancellation means the caller went away and is a
/// fact about your own process.
///
/// The obvious discriminator does not work. The exception's own
/// CancellationToken reports cancelled in BOTH cases, because HttpClient
/// implements its timeout by cancelling an internal token. What separates them:
/// a timeout carries a TimeoutException as its InnerException, and a
/// cancellation leaves the CALLER'S token cancelled.
///
/// ClassifyAsync sends a GET and returns Completed, Timeout or Cancelled.
/// </summary>
public static class TimeoutsAndCancellation
{
    public const string Completed = "completed";
    public const string Timeout = "timeout";
    public const string Cancelled = "cancelled";

    public static Task<string> ClassifyAsync(
        HttpClient client,
        string requestUri,
        CancellationToken cancellationToken)
        => throw new NotImplementedException();
}
