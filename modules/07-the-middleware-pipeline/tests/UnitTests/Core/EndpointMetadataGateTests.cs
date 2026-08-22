using Microsoft.AspNetCore.Http;
using Shouldly;
using Training.Module07.Core;

namespace Training.Module07.Tests.Core;

public sealed class EndpointMetadataGateTests
{
    [Fact]
    public async Task After_routing_the_gate_rejects_a_request_with_no_key()
    {
        var result = await PipelineHarness.SendAsync(
            EndpointMetadataGate.ConfigureGateAfterRouting, path: "/admin");

        result.StatusCode.ShouldBe(401);
        result.Body.ShouldBeEmpty();
    }

    [Fact]
    public async Task After_routing_a_request_with_a_key_reaches_the_endpoint()
    {
        var result = await PipelineHarness.SendAsync(
            EndpointMetadataGate.ConfigureGateAfterRouting,
            path: "/admin",
            prepare: context => context.Request.Headers[EndpointMetadataGate.HeaderName] = "let-me-in");

        result.StatusCode.ShouldBe(200);
        result.Body.ShouldBe("SECRET");
    }

    [Fact]
    public async Task An_endpoint_without_the_metadata_is_never_gated()
    {
        var result = await PipelineHarness.SendAsync(
            EndpointMetadataGate.ConfigureGateAfterRouting, path: "/health");

        result.StatusCode.ShouldBe(200);
        result.Body.ShouldBe("ok");
    }

    [Fact]
    public async Task An_ungated_endpoint_is_unaffected_by_a_key_being_present()
    {
        var result = await PipelineHarness.SendAsync(
            EndpointMetadataGate.ConfigureGateAfterRouting,
            path: "/health",
            prepare: context => context.Request.Headers[EndpointMetadataGate.HeaderName] = "let-me-in");

        result.Body.ShouldBe("ok");
    }

    [Fact]
    public async Task Before_routing_the_gate_leaves_the_protected_endpoint_wide_open()
    {
        // The bug the module exists to prevent. One line earlier in the
        // pipeline and the protected endpoint answers anybody, with no
        // exception, no log line, and a 200 that looks entirely healthy.
        var result = await PipelineHarness.SendAsync(
            EndpointMetadataGate.ConfigureGateBeforeRouting, path: "/admin");

        result.StatusCode.ShouldBe(200);
        result.Body.ShouldBe("SECRET");
    }

    [Fact]
    public async Task Because_before_routing_there_is_no_endpoint_to_ask_about()
    {
        Endpoint? seen = null;

        await PipelineHarness.SendAsync(
            app =>
            {
                app.Use(next => context =>
                {
                    seen = context.GetEndpoint();
                    return next(context);
                });
                EndpointMetadataGate.ConfigureGateBeforeRouting(app);
            },
            path: "/admin");

        seen.ShouldBeNull();
    }

    [Fact]
    public async Task The_open_and_the_protected_endpoint_answer_identically_when_broken()
    {
        // The detection gap: nothing in either response distinguishes the
        // working configuration from the broken one.
        var open = await PipelineHarness.SendAsync(
            EndpointMetadataGate.ConfigureGateBeforeRouting, path: "/health");
        var protectedRoute = await PipelineHarness.SendAsync(
            EndpointMetadataGate.ConfigureGateBeforeRouting, path: "/admin");

        open.StatusCode.ShouldBe(protectedRoute.StatusCode);
    }
}
