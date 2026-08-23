using System.Net;
using Shouldly;
using Training.Module08.Challenge;

namespace Training.Module08.Tests.Challenge;

public sealed class RetryHandlerTests
{
    private static CancellationToken Token => TestContext.Current.CancellationToken;

    [Fact]
    public async Task A_transient_failure_is_retried_until_it_succeeds()
    {
        using var primary = FailingTimes(2, HttpStatusCode.ServiceUnavailable);
        var retry = new RetryHandler(3) { InnerHandler = primary };
        using var client = ClientOver(retry);

        var response = await client.GetAsync("orders", Token);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        retry.Attempts.ShouldBe(3);
    }

    [Fact]
    public async Task A_persistent_failure_stops_at_the_attempt_limit()
    {
        using var primary = FailingTimes(int.MaxValue, HttpStatusCode.ServiceUnavailable);
        var retry = new RetryHandler(3) { InnerHandler = primary };
        using var client = ClientOver(retry);

        var response = await client.GetAsync("orders", Token);

        response.StatusCode.ShouldBe(HttpStatusCode.ServiceUnavailable);
        retry.Attempts.ShouldBe(3);
    }

    [Fact]
    public async Task A_POST_is_never_retried()
    {
        // The rule that matters most. A POST that failed may already have been
        // applied at the far end, so a retry is how one order becomes two.
        using var primary = FailingTimes(int.MaxValue, HttpStatusCode.ServiceUnavailable);
        var retry = new RetryHandler(3) { InnerHandler = primary };
        using var client = ClientOver(retry);

        await client.PostAsync("orders", new StringContent("{}"), Token);

        retry.Attempts.ShouldBe(1);
    }

    [Fact]
    public async Task A_client_error_is_not_retried_either()
    {
        using var primary = FailingTimes(int.MaxValue, HttpStatusCode.BadRequest);
        var retry = new RetryHandler(3) { InnerHandler = primary };
        using var client = ClientOver(retry);

        await client.GetAsync("orders", Token);

        retry.Attempts.ShouldBe(1);
    }

    [Fact]
    public async Task A_408_counts_as_transient()
    {
        using var primary = FailingTimes(1, HttpStatusCode.RequestTimeout);
        var retry = new RetryHandler(3) { InnerHandler = primary };
        using var client = ClientOver(retry);

        var response = await client.GetAsync("orders", Token);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        retry.Attempts.ShouldBe(2);
    }

    [Fact]
    public async Task Every_attempt_is_a_fresh_request_message()
    {
        // Reusing the message throws "already sent", so a retry handler that
        // loops over the same instance fails on its very first retry.
        using var primary = FailingTimes(2, HttpStatusCode.ServiceUnavailable);
        var retry = new RetryHandler(3) { InnerHandler = primary };
        using var client = ClientOver(retry);

        await client.GetAsync("orders", Token);

        primary.Requests.Count.ShouldBe(3);
        primary.Requests.Distinct().Count().ShouldBe(3);
    }

    [Fact]
    public async Task The_retried_request_keeps_its_headers()
    {
        using var primary = FailingTimes(1, HttpStatusCode.ServiceUnavailable);
        var retry = new RetryHandler(2) { InnerHandler = primary };
        using var client = ClientOver(retry);

        using var message = new HttpRequestMessage(HttpMethod.Get, "orders");
        message.Headers.Add("X-Correlation", "abc123");
        await client.SendAsync(message, Token);

        primary.Requests[1].Headers.GetValues("X-Correlation").ShouldBe(["abc123"]);
    }

    private static HttpClient ClientOver(HttpMessageHandler handler)
        => new(handler) { BaseAddress = new Uri("https://gateway.invalid/") };

    private static StubPrimaryHandler FailingTimes(int failures, HttpStatusCode status)
    {
        var seen = 0;

        return new StubPrimaryHandler((_, _) =>
        {
            var attempt = Interlocked.Increment(ref seen);
            var failing = attempt <= failures;

            return Task.FromResult(new HttpResponseMessage(failing ? status : HttpStatusCode.OK)
            {
                Content = new StringContent(failing ? "down" : "ok"),
            });
        });
    }
}
