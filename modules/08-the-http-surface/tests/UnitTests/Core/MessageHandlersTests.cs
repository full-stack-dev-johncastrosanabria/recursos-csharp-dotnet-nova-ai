using Shouldly;
using Training.Module08.Core;

namespace Training.Module08.Tests.Core;

public sealed class MessageHandlersTests
{
    private static CancellationToken Token => TestContext.Current.CancellationToken;


    [Fact]
    public async Task Handlers_run_outward_in_order_and_inward_in_reverse()
    {
        // The same onion as module 07, pointing at the network instead of at
        // your endpoint. The first handler in the list is the outermost.
        var log = new HandlerLog();
        using var primary = new StubPrimaryHandler();
        using var client = MessageHandlers.CreateClient(
            [new TracingHandler("auth", log), new TracingHandler("retry", log), new TracingHandler("logging", log)],
            primary);

        await client.GetAsync("orders", Token);

        log.Entries.ShouldBe([
            "out:auth", "out:retry", "out:logging", "in:logging", "in:retry", "in:auth"]);
    }

    [Fact]
    public async Task The_primary_handler_is_what_actually_sends()
    {
        var log = new HandlerLog();
        using var primary = new StubPrimaryHandler();
        using var client = MessageHandlers.CreateClient([new TracingHandler("only", log)], primary);

        await client.GetAsync("orders", Token);

        primary.Requests.Count.ShouldBe(1);
        primary.Requests[0].RequestUri!.ToString().ShouldBe("https://gateway.invalid/orders");
    }

    [Fact]
    public async Task A_chain_with_no_delegating_handlers_still_reaches_the_primary()
    {
        using var primary = new StubPrimaryHandler(body: "direct");
        using var client = MessageHandlers.CreateClient([], primary);

        var response = await client.GetAsync("orders", Token);

        (await response.Content.ReadAsStringAsync(Token)).ShouldBe("direct");
    }

    [Fact]
    public void Compose_returns_the_outermost_handler()
    {
        var log = new HandlerLog();
        var outer = new TracingHandler("outer", log);
        using var primary = new StubPrimaryHandler();

        var composed = MessageHandlers.Compose([outer, new TracingHandler("inner", log)], primary);

        composed.ShouldBeSameAs(outer);
    }

    [Fact]
    public async Task Reordering_the_list_reorders_the_chain()
    {
        // Registration order is the only thing deciding this. A retry handler
        // outside an auth handler retries with a stale token; inside it, the
        // token is refreshed per attempt. Same two classes, opposite meaning.
        var log = new HandlerLog();
        using var primary = new StubPrimaryHandler();
        using var client = MessageHandlers.CreateClient(
            [new TracingHandler("retry", log), new TracingHandler("auth", log)], primary);

        await client.GetAsync("orders", Token);

        log.Entries.ShouldBe(["out:retry", "out:auth", "in:auth", "in:retry"]);
    }
}
