using System.Net;
using Shouldly;
using Training.Module08.Core;

namespace Training.Module08.Tests.Core;

public sealed class ResponseHandlingTests
{
    private const string FailureBody = "gateway down: upstream pool exhausted";

    private static CancellationToken Token => TestContext.Current.CancellationToken;

    [Fact]
    public async Task A_success_is_reported_with_its_body()
    {
        using var client = ClientOver(HttpStatusCode.OK, "orders: 3");

        var result = await ResponseHandling.ReadAsync(client, "orders", Token);

        result.ShouldBe(new GatewayResult(true, 200, "orders: 3"));
    }

    [Fact]
    public async Task A_503_does_not_throw_and_its_body_survives()
    {
        // The behaviour people are surprised by. Nothing threw; the gateway
        // answered, and the answer was an error page.
        using var client = ClientOver(HttpStatusCode.ServiceUnavailable, FailureBody);

        var result = await ResponseHandling.ReadAsync(client, "orders", Token);

        result.Success.ShouldBeFalse();
        result.StatusCode.ShouldBe(503);
        result.Body.ShouldBe(FailureBody);
    }

    [Fact]
    public async Task A_404_is_an_answer_too()
    {
        using var client = ClientOver(HttpStatusCode.NotFound, "no such order");

        var result = await ResponseHandling.ReadAsync(client, "orders/9", Token);

        result.Success.ShouldBeFalse();
        result.StatusCode.ShouldBe(404);
    }

    [Fact]
    public async Task Throwing_deliberately_keeps_the_diagnostic()
    {
        using var client = ClientOver(HttpStatusCode.ServiceUnavailable, FailureBody);

        var thrown = await Should.ThrowAsync<HttpRequestException>(
            () => ResponseHandling.ReadOrThrowAsync(client, "orders", Token));

        thrown.StatusCode.ShouldBe(HttpStatusCode.ServiceUnavailable);
        thrown.Message.ShouldContain(FailureBody);
    }

    [Fact]
    public async Task EnsureSuccessStatusCode_throws_the_diagnostic_away()
    {
        // The contrast that matters. Same failure, same exception type, and
        // the one sentence that would have identified the cause is gone.
        using var client = ClientOver(HttpStatusCode.ServiceUnavailable, FailureBody);

        var thrown = await Should.ThrowAsync<HttpRequestException>(
            () => ResponseHandling.ReadWithEnsureAsync(client, "orders", Token));

        thrown.StatusCode.ShouldBe(HttpStatusCode.ServiceUnavailable);
        thrown.Message.ShouldNotContain(FailureBody);
    }

    [Fact]
    public async Task Both_throwing_forms_return_the_body_on_success()
    {
        using var first = ClientOver(HttpStatusCode.OK, "fine");
        using var second = ClientOver(HttpStatusCode.OK, "fine");

        (await ResponseHandling.ReadOrThrowAsync(first, "orders", Token)).ShouldBe("fine");
        (await ResponseHandling.ReadWithEnsureAsync(second, "orders", Token)).ShouldBe("fine");
    }

    private static HttpClient ClientOver(HttpStatusCode status, string body)
        => new(new StubPrimaryHandler(status, body))
        {
            BaseAddress = new Uri("https://gateway.invalid/"),
        };
}
