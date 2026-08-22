using Shouldly;
using Training.Module01.Core;

namespace Training.Module01.Tests.Core;

public sealed class OrderTotalsTests
{
    private static readonly List<Money> Lines =
    [
        new(10.00m, "USD"),
        new(4.50m, "USD"),
        new(0.50m, "USD"),
    ];

    /// <summary>
    /// Measures one call's allocations. The warm-up call matters: the first
    /// invocation pays for JIT compilation and static initialisation, which
    /// would otherwise be attributed to the method under test.
    /// </summary>
    private static long AllocatedBytes(Action action)
    {
        action();
        GC.Collect();
        GC.WaitForPendingFinalizers();

        var before = GC.GetAllocatedBytesForCurrentThread();
        action();
        return GC.GetAllocatedBytesForCurrentThread() - before;
    }

    [Fact]
    public void Both_versions_produce_the_same_total()
    {
        OrderTotals.SumWithoutAllocating(Lines).ShouldBe(15.00m);
        OrderTotals.SumViaInterface(Lines).ShouldBe(15.00m);
    }

    [Fact]
    public void Iterating_through_the_interface_allocates_where_the_concrete_list_does_not()
    {
        // foreach over IReadOnlyList<T> boxes List<T>'s struct enumerator.
        // The loop bodies are identical; only the parameter type differs.
        var viaInterface = AllocatedBytes(() => OrderTotals.SumViaInterface(Lines));
        var concrete = AllocatedBytes(() => OrderTotals.SumWithoutAllocating(Lines));

        viaInterface.ShouldBeGreaterThan(concrete);
    }

    [Fact]
    public void Iterating_the_concrete_list_allocates_nothing()
    {
        AllocatedBytes(() => OrderTotals.SumWithoutAllocating(Lines)).ShouldBe(0);
    }

    [Fact]
    public void An_empty_order_totals_zero()
    {
        OrderTotals.SumWithoutAllocating([]).ShouldBe(0m);
    }
}
