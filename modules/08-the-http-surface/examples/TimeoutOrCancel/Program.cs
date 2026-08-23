// Two different events arrive as the same exception type.
//
// A timeout means the dependency is too slow: a fact about them, and usually an
// incident. A cancellation means the caller went away: a fact about us, and
// usually nothing at all. Both surface as TaskCanceledException, so code that
// catches the type and logs "operation cancelled" reports an outage as routine.

using System.Net;

Console.WriteLine("Three calls. Two of them fail, and they are not the same failure.");
Console.WriteLine();
Console.WriteLine($"  {"scenario",-22}{"exception",-26}{"InnerException",-24}{"caller token",-14}");
Console.WriteLine("  " + new string('-', 86));

await Describe("responds promptly", TimeSpan.FromMilliseconds(10), Timeout.InfiniteTimeSpan, cancelAfter: null);
await Describe("client timeout", TimeSpan.FromSeconds(30), TimeSpan.FromMilliseconds(150), cancelAfter: null);
await Describe("caller cancels", TimeSpan.FromSeconds(30), Timeout.InfiniteTimeSpan, TimeSpan.FromMilliseconds(150));

Console.WriteLine();
Console.WriteLine("The exception type is identical, so it cannot be the discriminator. Nor");
Console.WriteLine("can the exception's own CancellationToken: HttpClient implements its");
Console.WriteLine("timeout by cancelling an internal token, so that token reports cancelled");
Console.WriteLine("in both rows.");
Console.WriteLine();
Console.WriteLine("Two things do work, and the table shows both. A timeout carries a");
Console.WriteLine("TimeoutException as its InnerException. And a real cancellation leaves");
Console.WriteLine("the CALLER'S token cancelled, which a timeout does not touch.");
Console.WriteLine();
Console.WriteLine("  catch (TaskCanceledException e) when (e.InnerException is TimeoutException)");
Console.WriteLine("      -> the dependency is too slow. Alert on this.");
Console.WriteLine();
Console.WriteLine("  catch (OperationCanceledException) when (callerToken.IsCancellationRequested)");
Console.WriteLine("      -> the caller left. Do not alert, and do not retry.");
Console.WriteLine();
Console.WriteLine("Getting this wrong costs in both directions: a dependency outage filed");
Console.WriteLine("as client churn, or a spike of user navigations paging somebody at 3am.");

static async Task Describe(string label, TimeSpan serverDelay, TimeSpan clientTimeout, TimeSpan? cancelAfter)
{
    using var client = new HttpClient(new SlowHandler(serverDelay)) { Timeout = clientTimeout };
    using var caller = new CancellationTokenSource();

    if (cancelAfter is { } delay)
    {
        caller.CancelAfter(delay);
    }

    try
    {
        using var response = await client.GetAsync("https://gateway.invalid/", caller.Token);
        Report(label, "none", "-", caller);
    }
    catch (Exception error)
    {
        Report(label, error.GetType().Name, error.InnerException?.GetType().Name ?? "null", caller);
    }
}

static void Report(string label, string exception, string inner, CancellationTokenSource caller)
    => Console.WriteLine(
        $"  {label,-22}{exception,-26}{inner,-24}{(caller.IsCancellationRequested ? "cancelled" : "live"),-14}");

internal sealed class SlowHandler(TimeSpan delay) : HttpMessageHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        await Task.Delay(delay, cancellationToken);

        return new HttpResponseMessage(HttpStatusCode.OK);
    }
}
