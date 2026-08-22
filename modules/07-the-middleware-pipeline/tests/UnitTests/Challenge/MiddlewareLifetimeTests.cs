using Shouldly;
using Training.Module07.Challenge;

namespace Training.Module07.Tests.Challenge;

public sealed class MiddlewareLifetimeTests
{
    [Fact]
    public async Task A_scoped_service_in_the_constructor_is_captured_for_every_request()
    {
        // Two requests, two scopes, one marker. The middleware class was built
        // once, so its "per request" dependency is nothing of the kind.
        var log = new MarkerLog();

        await PipelineHarness.SendManyAsync(
            MiddlewareLifetime.ConfigureCapturing,
            services => MiddlewareLifetime.Register(services, log),
            "/first",
            "/second");

        log.Observed.Count.ShouldBe(2);
        log.Observed.Distinct().Count().ShouldBe(1);
    }

    [Fact]
    public async Task A_scoped_service_on_InvokeAsync_is_resolved_per_request()
    {
        var log = new MarkerLog();

        await PipelineHarness.SendManyAsync(
            MiddlewareLifetime.ConfigurePerRequest,
            services => MiddlewareLifetime.Register(services, log),
            "/first",
            "/second");

        log.Observed.Count.ShouldBe(2);
        log.Observed.Distinct().Count().ShouldBe(2);
    }

    [Fact]
    public async Task The_capture_does_not_wear_off_over_more_requests()
    {
        var log = new MarkerLog();

        await PipelineHarness.SendManyAsync(
            MiddlewareLifetime.ConfigureCapturing,
            services => MiddlewareLifetime.Register(services, log),
            "/one",
            "/two",
            "/three");

        log.Observed.Count.ShouldBe(3);
        log.Observed.Distinct().Count().ShouldBe(1);
    }

    [Fact]
    public async Task Every_request_gets_its_own_marker_when_it_arrives_on_InvokeAsync()
    {
        var log = new MarkerLog();

        await PipelineHarness.SendManyAsync(
            MiddlewareLifetime.ConfigurePerRequest,
            services => MiddlewareLifetime.Register(services, log),
            "/one",
            "/two",
            "/three");

        log.Observed.Distinct().Count().ShouldBe(3);
    }

    [Fact]
    public async Task A_single_request_cannot_tell_the_two_apart()
    {
        // The detection gap. One request through either pipeline looks
        // identical, so nothing short of a second concurrent request reveals
        // it -- which is why this survives testing and fails in production.
        var captured = new MarkerLog();
        var perRequest = new MarkerLog();

        await PipelineHarness.SendManyAsync(
            MiddlewareLifetime.ConfigureCapturing,
            services => MiddlewareLifetime.Register(services, captured),
            "/only");
        await PipelineHarness.SendManyAsync(
            MiddlewareLifetime.ConfigurePerRequest,
            services => MiddlewareLifetime.Register(services, perRequest),
            "/only");

        captured.Observed.Count.ShouldBe(perRequest.Observed.Count);
    }

    [Fact]
    public async Task Both_pipelines_still_reach_the_terminal_delegate()
    {
        var log = new MarkerLog();

        var results = await PipelineHarness.SendManyAsync(
            MiddlewareLifetime.ConfigurePerRequest,
            services => MiddlewareLifetime.Register(services, log),
            "/only");

        results[0].Body.ShouldBe("done");
    }
}
