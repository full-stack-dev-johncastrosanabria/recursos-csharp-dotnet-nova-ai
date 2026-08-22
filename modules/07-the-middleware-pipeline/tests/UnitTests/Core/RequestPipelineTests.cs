using Microsoft.AspNetCore.Http;
using Shouldly;
using Training.Module07.Core;

namespace Training.Module07.Tests.Core;

public sealed class RequestPipelineTests
{
    [Fact]
    public async Task With_no_components_the_terminal_delegate_is_the_pipeline()
    {
        var log = new List<string>();

        var pipeline = RequestPipeline.Compose([], Recording(log, "terminal"));
        await pipeline(new DefaultHttpContext());

        log.ShouldBe(["terminal"]);
    }

    [Fact]
    public async Task Components_run_in_registration_order_on_the_way_in()
    {
        var log = new List<string>();

        var pipeline = RequestPipeline.Compose(
            [Wrapping(log, "one"), Wrapping(log, "two"), Wrapping(log, "three")],
            Recording(log, "terminal"));
        await pipeline(new DefaultHttpContext());

        log.Take(4).ShouldBe(["in:one", "in:two", "in:three", "terminal"]);
    }

    [Fact]
    public async Task And_in_reverse_order_on_the_way_out()
    {
        // The shape people call the onion. Everything after the await unwinds
        // in the opposite order, which is why a response-editing middleware
        // must sit OUTSIDE (earlier than) whatever writes the response.
        var log = new List<string>();

        var pipeline = RequestPipeline.Compose(
            [Wrapping(log, "one"), Wrapping(log, "two"), Wrapping(log, "three")],
            Recording(log, "terminal"));
        await pipeline(new DefaultHttpContext());

        log.ShouldBe([
            "in:one", "in:two", "in:three", "terminal", "out:three", "out:two", "out:one"]);
    }

    [Fact]
    public async Task A_component_that_never_calls_next_stops_everything_behind_it()
    {
        var log = new List<string>();

        var pipeline = RequestPipeline.Compose(
            [Wrapping(log, "one"), StoppingAt(log, "guard"), Wrapping(log, "three")],
            Recording(log, "terminal"));
        await pipeline(new DefaultHttpContext());

        // "three" and the terminal never ran. "one" still unwinds, because it
        // did call next -- a short circuit downstream is invisible to it.
        log.ShouldBe(["in:one", "guard", "out:one"]);
    }

    [Fact]
    public async Task A_single_component_wraps_the_terminal_delegate()
    {
        var log = new List<string>();

        var pipeline = RequestPipeline.Compose([Wrapping(log, "only")], Recording(log, "terminal"));
        await pipeline(new DefaultHttpContext());

        log.ShouldBe(["in:only", "terminal", "out:only"]);
    }

    [Fact]
    public async Task Reversing_the_list_reverses_the_pipeline()
    {
        // Registration order IS execution order. Nothing else decides it --
        // no priority, no attribute, no sorting.
        var log = new List<string>();

        var pipeline = RequestPipeline.Compose(
            [Wrapping(log, "two"), Wrapping(log, "one")], Recording(log, "terminal"));
        await pipeline(new DefaultHttpContext());

        log.ShouldBe(["in:two", "in:one", "terminal", "out:one", "out:two"]);
    }

    private static RequestDelegate Recording(List<string> log, string name)
        => _ =>
        {
            log.Add(name);
            return Task.CompletedTask;
        };

    private static Func<RequestDelegate, RequestDelegate> Wrapping(List<string> log, string name)
        => next => async context =>
        {
            log.Add($"in:{name}");
            await next(context);
            log.Add($"out:{name}");
        };

    private static Func<RequestDelegate, RequestDelegate> StoppingAt(List<string> log, string name)
        => next => context =>
        {
            log.Add(name);
            return Task.CompletedTask;
        };
}
