using Shouldly;
using Training.Module01.Challenge;

namespace Training.Module01.Tests.Challenge;

public sealed class ComparisonsTests
{
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
    public void Returns_the_larger_value()
    {
        Comparisons.Max(3, 7).ShouldBe(7);
        Comparisons.Max("apple", "banana").ShouldBe("banana");
    }

    [Fact]
    public void Returns_the_left_value_when_they_are_equal()
    {
        Comparisons.Max(5, 5).ShouldBe(5);
    }

    [Fact]
    public void The_generic_version_does_not_box_value_types()
    {
        AllocatedBytes(() => Comparisons.Max(3, 7)).ShouldBe(0);
    }

    [Fact]
    public void The_interface_version_boxes_where_the_generic_one_does_not()
    {
        // Both do the same comparison. The non-generic parameters force each int
        // onto the heap before the call can be made at all.
        var viaInterface = AllocatedBytes(() => Comparisons.MaxViaInterface(3, 7));
        var generic = AllocatedBytes(() => Comparisons.Max(3, 7));

        viaInterface.ShouldBeGreaterThan(generic);
    }
}
