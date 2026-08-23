namespace Training.Module08.Core;

/// <summary>
/// Telling a timeout apart from a cancellation, both of which arrive as
/// TaskCanceledException.
/// </summary>
public static class TimeoutsAndCancellation
{
    public const string Completed = "completed";
    public const string Timeout = "timeout";
    public const string Cancelled = "cancelled";

    public static async Task<string> ClassifyAsync(
        HttpClient client,
        string requestUri,
        CancellationToken cancellationToken)
    {
        try
        {
            using var response = await client.GetAsync(requestUri, cancellationToken);

            return Completed;
        }
        catch (TaskCanceledException error) when (error.InnerException is TimeoutException)
        {
            // HttpClient.Timeout elapsed. A fact about the dependency.
            return Timeout;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // The caller went away. A fact about us.
            return Cancelled;
        }
    }
}
