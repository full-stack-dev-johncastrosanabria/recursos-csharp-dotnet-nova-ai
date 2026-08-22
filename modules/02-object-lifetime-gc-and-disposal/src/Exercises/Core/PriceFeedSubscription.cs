namespace Training.Module02.Core;

/// <summary>A long-lived publisher of price changes. Given to you, not part of the exercise.</summary>
public sealed class PriceFeed
{
    public event EventHandler<decimal>? PriceChanged;

    public int SubscriberCount => PriceChanged?.GetInvocationList().Length ?? 0;

    public void Publish(decimal price) => PriceChanged?.Invoke(this, price);
}

/// <summary>
/// Watches a price feed for as long as it is alive.
///
/// Exercise: subscribe on construction and unsubscribe on disposal. This is the
/// most common managed leak in .NET: the publisher outlives the subscriber and
/// holds a reference to its handler, which holds the subscriber, which holds
/// everything the subscriber holds. Nothing is garbage, so no amount of
/// collecting frees it.
/// </summary>
public sealed class PriceFeedSubscription : IDisposable
{
    public PriceFeedSubscription(PriceFeed feed) => throw new NotImplementedException();

    public int Received => throw new NotImplementedException();

    public decimal Last => throw new NotImplementedException();

    public void Dispose() => throw new NotImplementedException();
}
