using Shouldly;
using Training.Module07.Core;

namespace Training.Module07.Tests.Core;

public sealed class ExceptionHandlingOrderTests
{
    [Fact]
    public async Task A_handler_registered_first_catches_what_comes_after_it()
    {
        var log = new List<string>();

        var result = await PipelineHarness.SendAsync(
            app => ExceptionHandlingOrder.ConfigureHandlerFirst(app, log));

        result.StatusCode.ShouldBe(500);
        log.ShouldBe(["threw", "caught"]);
    }

    [Fact]
    public async Task The_handler_leaves_no_partial_response_behind()
    {
        var log = new List<string>();

        var result = await PipelineHarness.SendAsync(
            app => ExceptionHandlingOrder.ConfigureHandlerFirst(app, log));

        result.Body.ShouldBeEmpty();
    }

    [Fact]
    public async Task A_handler_registered_last_never_sees_the_failure()
    {
        // The thrower is not inside the handler's try block, so the exception
        // leaves the pipeline entirely. In a real host that is a connection
        // reset rather than a response.
        var log = new List<string>();

        var thrown = await Should.ThrowAsync<InvalidOperationException>(
            () => PipelineHarness.SendAsync(app => ExceptionHandlingOrder.ConfigureHandlerLast(app, log)));

        thrown.Message.ShouldBe("checkout failed");
        log.ShouldBe(["threw"]);
        log.ShouldNotContain("caught");
    }
}
