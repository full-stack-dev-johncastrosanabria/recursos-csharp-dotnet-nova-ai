namespace Training.Module02.Core;

/// <summary>
/// A hold on stock for one order line, released when the lease is disposed.
///
/// Exercise: implement disposal properly. Releasing must happen exactly once
/// however many times Dispose is called — callers cannot always know whether a
/// `using` already ran, and releasing stock twice oversells it. Using the lease
/// after disposal must throw rather than quietly do nothing.
/// </summary>
public sealed class ReservationLease : IDisposable
{
    private readonly Action<string, int> _release;

    public ReservationLease(string sku, int quantity, Action<string, int> release)
    {
        Sku = sku;
        Quantity = quantity;
        _release = release;
    }

    public string Sku { get; }

    public int Quantity { get; }

    public bool IsDisposed => throw new NotImplementedException();

    public int Renewals => throw new NotImplementedException();

    public void Renew() => throw new NotImplementedException();

    public void Dispose() => throw new NotImplementedException();
}
