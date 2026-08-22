using Shouldly;
using Training.Module07.Core;

namespace Training.Module07.Tests.Core;

public sealed class PipelineBranchingTests
{
    [Fact]
    public async Task Map_sends_the_matching_prefix_down_a_branch_that_never_returns()
    {
        var log = new List<string>();

        var result = await PipelineHarness.SendAsync(
            app => PipelineBranching.ConfigureMap(app, log), path: "/api/orders");

        result.Body.ShouldBe("api");
        log.ShouldBe(["branch:/api|/orders"]);
        log.ShouldNotContain("main");
    }

    [Fact]
    public async Task A_non_matching_path_never_enters_the_branch()
    {
        var log = new List<string>();

        var result = await PipelineHarness.SendAsync(
            app => PipelineBranching.ConfigureMap(app, log), path: "/orders");

        result.Body.ShouldBe("main");
        log.ShouldBe(["main"]);
    }

    [Fact]
    public async Task Map_moves_the_matched_segment_from_Path_onto_PathBase()
    {
        // Worth meeting once: inside the branch the request no longer knows it
        // was "/api/orders", which breaks any downstream code that reads
        // Request.Path expecting the original.
        var log = new List<string>();

        await PipelineHarness.SendAsync(
            app => PipelineBranching.ConfigureMap(app, log), path: "/api/orders");

        log.ShouldBe(["branch:/api|/orders"]);
    }

    [Fact]
    public async Task MapWhen_does_not_rewrite_the_path()
    {
        // The asymmetry that catches people: Map consumes the segment it
        // matched, MapWhen matched on something else entirely and leaves the
        // request exactly as it found it.
        var log = new List<string>();

        await PipelineHarness.SendAsync(
            app => PipelineBranching.ConfigureMapWhen(app, log),
            path: "/orders",
            prepare: context => context.Request.Headers[PipelineBranching.BranchHeader] = "1");

        log.ShouldBe(["branch:|/orders"]);
    }

    [Fact]
    public async Task MapWhen_branches_on_anything_about_the_request_and_also_never_returns()
    {
        var log = new List<string>();

        var result = await PipelineHarness.SendAsync(
            app => PipelineBranching.ConfigureMapWhen(app, log),
            prepare: context => context.Request.Headers[PipelineBranching.BranchHeader] = "1");

        result.Body.ShouldBe("api");
        log.Count.ShouldBe(1);
        log[0].ShouldStartWith("branch:");
    }

    [Fact]
    public async Task UseWhen_rejoins_the_main_pipeline()
    {
        // The difference that matters. MapWhen replaces the rest of the
        // pipeline; UseWhen inserts into it.
        var log = new List<string>();

        var result = await PipelineHarness.SendAsync(
            app => PipelineBranching.ConfigureUseWhen(app, log),
            prepare: context => context.Request.Headers[PipelineBranching.BranchHeader] = "1");

        result.Body.ShouldBe("main");
        log.ShouldBe(["branch", "main"]);
    }

    [Fact]
    public async Task UseWhen_leaves_a_non_matching_request_untouched()
    {
        var log = new List<string>();

        await PipelineHarness.SendAsync(app => PipelineBranching.ConfigureUseWhen(app, log));

        log.ShouldBe(["main"]);
    }
}
