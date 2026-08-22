using Shouldly;
using Training.Module07.Challenge;

namespace Training.Module07.Tests.Challenge;

public sealed class PerRequestStateTests
{
    [Fact]
    public async Task A_field_on_a_middleware_carries_one_requests_data_into_the_next()
    {
        // The second request found the first request's path waiting for it.
        // Nothing here is concurrent: the leak does not need a race, it only
        // needs two requests.
        var log = new StateLog();

        await PipelineHarness.SendManyAsync(
            PerRequestState.ConfigureFieldState,
            services => PerRequestState.Register(services, log),
            "/first",
            "/second");

        log.FoundOnEntry.ShouldBe(["", "/first"]);
    }

    [Fact]
    public async Task HttpContext_Items_does_not_survive_the_request()
    {
        var log = new StateLog();

        await PipelineHarness.SendManyAsync(
            PerRequestState.ConfigureContextItems,
            services => PerRequestState.Register(services, log),
            "/first",
            "/second");

        log.FoundOnEntry.ShouldBe(["", ""]);
    }

    [Fact]
    public async Task The_leak_compounds_across_every_request_that_follows()
    {
        var log = new StateLog();

        await PipelineHarness.SendManyAsync(
            PerRequestState.ConfigureFieldState,
            services => PerRequestState.Register(services, log),
            "/first",
            "/second",
            "/third");

        log.FoundOnEntry.ShouldBe(["", "/first", "/second"]);
    }

    [Fact]
    public async Task Items_stays_empty_however_many_requests_arrive()
    {
        var log = new StateLog();

        await PipelineHarness.SendManyAsync(
            PerRequestState.ConfigureContextItems,
            services => PerRequestState.Register(services, log),
            "/first",
            "/second",
            "/third");

        log.FoundOnEntry.ShouldBe(["", "", ""]);
    }

    [Fact]
    public async Task Both_pipelines_still_answer_the_request()
    {
        var log = new StateLog();

        var results = await PipelineHarness.SendManyAsync(
            PerRequestState.ConfigureContextItems,
            services => PerRequestState.Register(services, log),
            "/only");

        results[0].Body.ShouldBe("done");
    }
}
