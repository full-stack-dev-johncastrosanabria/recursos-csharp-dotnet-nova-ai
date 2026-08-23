using Shouldly;
using Training.Module08.Core;

namespace Training.Module08.Tests.Core;

public sealed class ClientLifetimeTests
{
    private static CancellationToken Token => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Disposing_an_owning_client_destroys_the_handler_underneath_it()
    {
        var handler = new TrackingPrimaryHandler();
        var client = ClientLifetime.CreateOwning(handler);

        await client.GetAsync("orders", Token);
        client.Dispose();

        handler.Disposed.ShouldBeTrue();
    }

    [Fact]
    public async Task Disposing_a_borrowing_client_leaves_the_handler_alone()
    {
        var handler = new TrackingPrimaryHandler();
        var client = ClientLifetime.CreateBorrowing(handler);

        await client.GetAsync("orders", Token);
        client.Dispose();

        handler.Disposed.ShouldBeFalse();
    }

    [Fact]
    public async Task Several_borrowing_clients_share_one_handler()
    {
        // This is what a pooled client looks like: cheap facades over one
        // expensive, long-lived handler that owns the connections.
        var handler = new TrackingPrimaryHandler();

        using (var first = ClientLifetime.CreateBorrowing(handler))
        {
            await first.GetAsync("orders", Token);
        }

        using (var second = ClientLifetime.CreateBorrowing(handler))
        {
            await second.GetAsync("orders", Token);
        }

        handler.SendCount.ShouldBe(2);
        handler.Disposed.ShouldBeFalse();
    }

    [Fact]
    public async Task A_disposed_handler_cannot_serve_the_next_client()
    {
        // The cost of getting ownership wrong: the second client inherits a
        // corpse. In production the equivalent is a rebuilt connection pool.
        var handler = new TrackingPrimaryHandler();
        ClientLifetime.CreateOwning(handler).Dispose();

        using var next = ClientLifetime.CreateBorrowing(handler);

        await Should.ThrowAsync<ObjectDisposedException>(() => next.GetAsync("orders", Token));
    }

    [Fact]
    public async Task An_owning_client_still_serves_requests_normally_until_disposed()
    {
        var handler = new TrackingPrimaryHandler();
        using var client = ClientLifetime.CreateOwning(handler);

        var response = await client.GetAsync("orders", Token);

        (await response.Content.ReadAsStringAsync(Token)).ShouldBe("ok");
        handler.Disposed.ShouldBeFalse();
    }
}
