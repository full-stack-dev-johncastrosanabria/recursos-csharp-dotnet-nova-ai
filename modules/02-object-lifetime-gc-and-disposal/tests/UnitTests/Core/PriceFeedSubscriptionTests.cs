using Shouldly;
using Training.Module02.Core;

namespace Training.Module02.Tests.Core;

public sealed class PriceFeedSubscriptionTests
{
    [Fact]
    public void A_live_subscription_receives_prices()
    {
        var feed = new PriceFeed();
        using var subscription = new PriceFeedSubscription(feed);

        feed.Publish(19.99m);

        subscription.Received.ShouldBe(1);
        subscription.Last.ShouldBe(19.99m);
    }

    [Fact]
    public void Disposing_stops_delivery()
    {
        var feed = new PriceFeed();
        var subscription = new PriceFeedSubscription(feed);

        feed.Publish(1m);
        subscription.Dispose();
        feed.Publish(2m);

        subscription.Received.ShouldBe(1);
    }

    [Fact]
    public void Disposing_detaches_the_handler_from_the_publisher()
    {
        // This is the leak. The feed is long-lived and holds a reference to the
        // handler, which holds the subscriber. Forget to unsubscribe and the
        // subscriber stays reachable forever -- and so does everything it
        // holds. Nothing is garbage, so the GC is powerless.
        var feed = new PriceFeed();
        var subscription = new PriceFeedSubscription(feed);

        feed.SubscriberCount.ShouldBe(1);
        subscription.Dispose();

        feed.SubscriberCount.ShouldBe(0);
    }

    [Fact]
    public void Disposing_twice_does_not_detach_someone_elses_handler()
    {
        var feed = new PriceFeed();
        var first = new PriceFeedSubscription(feed);
        var second = new PriceFeedSubscription(feed);

        first.Dispose();
        first.Dispose();

        feed.SubscriberCount.ShouldBe(1);
        second.Dispose();
        feed.SubscriberCount.ShouldBe(0);
    }

    [Fact]
    public void Each_subscription_tracks_only_its_own_prices()
    {
        var feed = new PriceFeed();
        using var first = new PriceFeedSubscription(feed);

        feed.Publish(5m);

        using var second = new PriceFeedSubscription(feed);
        feed.Publish(7m);

        first.Received.ShouldBe(2);
        second.Received.ShouldBe(1);
        second.Last.ShouldBe(7m);
    }
}
