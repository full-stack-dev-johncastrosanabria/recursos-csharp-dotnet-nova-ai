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
/// The handler is stored in a field rather than written inline at both the +=
/// and the -=. Two separately written lambdas are two different delegate
/// instances, so the -= silently removes nothing and the leak survives code
/// that looks like it unsubscribes.
/// </summary>
public sealed class PriceFeedSubscription : IDisposable
{
    private readonly PriceFeed _feed;
    private readonly EventHandler<decimal> _handler;
    private bool _disposed;
    private int _received;
    private decimal _last;

    public PriceFeedSubscription(PriceFeed feed)
    {
        _feed = feed;
        _handler = OnPriceChanged;
        _feed.PriceChanged += _handler;
    }

    public int Received => _received;

    public decimal Last => _last;

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _feed.PriceChanged -= _handler;
    }

    private void OnPriceChanged(object? sender, decimal price)
    {
        _received++;
        _last = price;
    }
}
