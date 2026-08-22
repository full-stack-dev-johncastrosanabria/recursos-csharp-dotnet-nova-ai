using Shouldly;
using Training.Module07.Core;

namespace Training.Module07.Tests.Core;

public sealed class ShortCircuitingTests
{
    [Fact]
    public async Task A_terminal_delegate_ends_the_chain_and_the_rest_unwinds()
    {
        var log = new List<string>();

        var result = await PipelineHarness.SendAsync(app => ShortCircuiting.ConfigureWithTerminal(app, log));

        log.ShouldBe(["in:one", "in:two", "terminal", "out:two", "out:one"]);
        result.Body.ShouldBe("handled");
    }

    [Fact]
    public async Task A_guard_that_does_not_call_next_stops_the_request()
    {
        var log = new List<string>();

        var result = await PipelineHarness.SendAsync(app => ShortCircuiting.ConfigureWithGuard(app, log));

        result.StatusCode.ShouldBe(403);
        result.Body.ShouldBeEmpty();
        log.ShouldNotContain("terminal");
    }

    [Fact]
    public async Task But_everything_outside_the_guard_still_unwinds()
    {
        // "one" called next, so its second half runs. A short circuit deeper in
        // the pipeline is indistinguishable, from outside, from a completed
        // request -- which is why logging middleware reports both as success.
        var log = new List<string>();

        await PipelineHarness.SendAsync(app => ShortCircuiting.ConfigureWithGuard(app, log));

        log.ShouldBe(["in:one", "guard", "out:one"]);
    }

    [Fact]
    public async Task A_completed_pipeline_leaves_the_default_status()
    {
        // Nothing set a status, so it is 200. A short circuit that forgets to
        // set one therefore reports success -- see the guard test above, which
        // has to set 403 explicitly.
        var log = new List<string>();

        var result = await PipelineHarness.SendAsync(app => ShortCircuiting.ConfigureWithTerminal(app, log));

        result.StatusCode.ShouldBe(200);
    }

    [Fact]
    public async Task Anything_registered_after_a_terminal_delegate_is_unreachable()
    {
        var log = new List<string>();

        await PipelineHarness.SendAsync(app => ShortCircuiting.ConfigureAfterTerminal(app, log));

        log.ShouldBe(["terminal"]);
        log.ShouldNotContain("in:late");
    }
}
