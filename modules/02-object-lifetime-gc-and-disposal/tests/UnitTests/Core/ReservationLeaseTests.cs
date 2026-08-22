using Shouldly;
using Training.Module02.Core;

namespace Training.Module02.Tests.Core;

public sealed class ReservationLeaseTests
{
    [Fact]
    public void Disposing_releases_the_reservation()
    {
        var released = new List<(string Sku, int Quantity)>();
        var lease = new ReservationLease("SKU-1", 3, (sku, quantity) => released.Add((sku, quantity)));

        lease.Dispose();

        released.ShouldBe([("SKU-1", 3)]);
    }

    [Fact]
    public void Disposing_twice_releases_once()
    {
        // Dispose must be idempotent. Callers cannot always know whether a
        // `using` already ran, and releasing stock twice oversells it.
        var released = new List<(string Sku, int Quantity)>();
        var lease = new ReservationLease("SKU-1", 3, (sku, quantity) => released.Add((sku, quantity)));

        lease.Dispose();
        lease.Dispose();

        released.Count.ShouldBe(1);
    }

    [Fact]
    public void A_using_block_releases_at_the_end_of_scope()
    {
        var released = new List<(string Sku, int Quantity)>();

        using (var lease = new ReservationLease("SKU-2", 1, (sku, quantity) => released.Add((sku, quantity))))
        {
            lease.IsDisposed.ShouldBeFalse();
            released.ShouldBeEmpty();
        }

        released.ShouldBe([("SKU-2", 1)]);
    }

    [Fact]
    public void IsDisposed_reports_the_state()
    {
        var lease = new ReservationLease("SKU-3", 2, (_, _) => { });

        lease.IsDisposed.ShouldBeFalse();
        lease.Dispose();
        lease.IsDisposed.ShouldBeTrue();
    }

    [Fact]
    public void Renewing_a_live_lease_is_allowed()
    {
        using var lease = new ReservationLease("SKU-4", 1, (_, _) => { });

        Should.NotThrow(lease.Renew);
        lease.Renewals.ShouldBe(1);
    }

    [Fact]
    public void Renewing_after_disposal_throws_rather_than_pretending()
    {
        var lease = new ReservationLease("SKU-5", 1, (_, _) => { });
        lease.Dispose();

        Should.Throw<ObjectDisposedException>(lease.Renew);
    }
}
