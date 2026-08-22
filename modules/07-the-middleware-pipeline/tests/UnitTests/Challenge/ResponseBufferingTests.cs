using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Shouldly;
using Training.Module07.Challenge;

namespace Training.Module07.Tests.Challenge;

public sealed class ResponseBufferingTests
{
    [Fact]
    public async Task The_middleware_sees_what_the_terminal_delegate_wrote()
    {
        var captured = new List<string>();

        await PipelineHarness.SendAsync(app => ResponseBuffering.Configure(app, captured));

        captured.ShouldBe(["hello"]);
    }

    [Fact]
    public async Task And_the_caller_still_receives_it()
    {
        // The half people miss. Capturing the body is easy; giving it back is
        // the part that makes the difference between a logging middleware and
        // an outage in which every response is empty.
        var captured = new List<string>();

        var result = await PipelineHarness.SendAsync(app => ResponseBuffering.Configure(app, captured));

        result.Body.ShouldBe("hello");
    }

    [Fact]
    public async Task Several_writes_are_captured_as_one_body()
    {
        var captured = new List<string>();

        var result = await PipelineHarness.SendAsync(app =>
        {
            ResponseBuffering.UseResponseCapture(app, captured);
            app.Run(async context =>
            {
                await context.Response.WriteAsync("one ");
                await context.Response.WriteAsync("two");
            });
        });

        captured.ShouldBe(["one two"]);
        result.Body.ShouldBe("one two");
    }

    [Fact]
    public async Task An_empty_response_is_captured_as_an_empty_string()
    {
        var captured = new List<string>();

        await PipelineHarness.SendAsync(app =>
        {
            ResponseBuffering.UseResponseCapture(app, captured);
            app.Run(context =>
            {
                context.Response.StatusCode = 204;
                return Task.CompletedTask;
            });
        });

        captured.ShouldBe([string.Empty]);
    }

    [Fact]
    public async Task The_status_code_is_untouched_by_the_capture()
    {
        var captured = new List<string>();

        var result = await PipelineHarness.SendAsync(app =>
        {
            ResponseBuffering.UseResponseCapture(app, captured);
            app.Run(context =>
            {
                context.Response.StatusCode = 418;
                return context.Response.WriteAsync("teapot");
            });
        });

        result.StatusCode.ShouldBe(418);
        captured.ShouldBe(["teapot"]);
    }

    [Fact]
    public async Task The_original_response_stream_is_put_back()
    {
        // A middleware registered outside the capture must find the stream it
        // started with. Forgetting to restore it is invisible until something
        // upstream writes and the bytes vanish.
        Stream? before = null;
        Stream? after = null;
        var captured = new List<string>();

        await PipelineHarness.SendAsync(app =>
        {
            app.Use(async (context, next) =>
            {
                before = context.Response.Body;
                await next(context);
                after = context.Response.Body;
            });
            ResponseBuffering.Configure(app, captured);
        });

        after.ShouldBeSameAs(before);
    }
}
