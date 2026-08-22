using Shouldly;
using Training.Module01.Challenge;

namespace Training.Module01.Tests.Challenge;

public sealed class SkuListTests
{
    private static readonly string[] Skus = ["SKU-1", "SKU-2", "SKU-3"];

    private static long AllocatedBytes(Action action)
    {
        action();
        GC.Collect();
        GC.WaitForPendingFinalizers();

        var before = GC.GetAllocatedBytesForCurrentThread();
        action();
        return GC.GetAllocatedBytesForCurrentThread() - before;
    }

    private static int CountByEnumerating(SkuList list)
    {
        var seen = 0;
        foreach (var sku in list)
        {
            if (sku.Length > 0)
            {
                seen++;
            }
        }

        return seen;
    }

    [Fact]
    public void Enumerates_every_item_in_order()
    {
        var list = new SkuList(Skus);
        var collected = new List<string>();

        foreach (var sku in list)
        {
            collected.Add(sku);
        }

        collected.ShouldBe(Skus);
    }

    [Fact]
    public void Exposes_its_count()
    {
        new SkuList(Skus).Count.ShouldBe(3);
    }

    [Fact]
    public void Enumerating_allocates_nothing()
    {
        // foreach binds to the struct GetEnumerator by pattern, never through
        // IEnumerable<T>, so nothing is boxed.
        var list = new SkuList(Skus);

        AllocatedBytes(() => CountByEnumerating(list)).ShouldBe(0);
    }

    [Fact]
    public void An_empty_list_enumerates_zero_times()
    {
        CountByEnumerating(new SkuList([])).ShouldBe(0);
    }
}
