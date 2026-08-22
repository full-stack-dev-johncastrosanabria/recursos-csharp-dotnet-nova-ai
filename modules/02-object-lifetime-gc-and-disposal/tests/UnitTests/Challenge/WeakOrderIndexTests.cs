using System.Runtime.CompilerServices;
using Shouldly;
using Training.Module02.Challenge;

namespace Training.Module02.Tests.Challenge;

public sealed class WeakOrderIndexTests
{
    /// <summary>
    /// Adds an entry and keeps no reference to it. This must be its own
    /// non-inlined method: a local in the calling test can stay rooted for the
    /// whole method body, and the collection would not happen.
    /// </summary>
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void AddAndForget(WeakOrderIndex index, string orderId)
        => index.Add(orderId, new OrderDocument(orderId));

    private static void Collect()
    {
        GC.Collect(2, GCCollectionMode.Forced, blocking: true);
        GC.WaitForPendingFinalizers();
        GC.Collect(2, GCCollectionMode.Forced, blocking: true);
    }

    [Fact]
    public void Returns_a_document_that_is_still_referenced()
    {
        var index = new WeakOrderIndex();
        var document = new OrderDocument("ord_1");
        index.Add("ord_1", document);

        Collect();

        index.TryGet("ord_1", out var found).ShouldBeTrue();
        found.ShouldBeSameAs(document);
    }

    [Fact]
    public void Forgets_a_document_nobody_else_holds()
    {
        // The index does not keep the document alive. That is the entire point:
        // a cache built on strong references is the leak this module is about.
        var index = new WeakOrderIndex();
        AddAndForget(index, "ord_2");

        Collect();

        index.TryGet("ord_2", out var found).ShouldBeFalse();
        found.ShouldBeNull();
    }

    [Fact]
    public void A_missing_key_is_a_miss_rather_than_a_throw()
    {
        var index = new WeakOrderIndex();

        index.TryGet("never-added", out var found).ShouldBeFalse();
        found.ShouldBeNull();
    }

    [Fact]
    public void Count_still_includes_entries_whose_document_is_gone()
    {
        // The weak reference dies; the dictionary entry holding it does not.
        // An index like this leaks its own bookkeeping unless it is pruned.
        var index = new WeakOrderIndex();
        AddAndForget(index, "ord_3");

        Collect();

        index.Count.ShouldBe(1);
    }

    [Fact]
    public void Pruning_removes_the_dead_entries_and_reports_how_many()
    {
        var index = new WeakOrderIndex();
        var kept = new OrderDocument("live");
        index.Add("live", kept);
        AddAndForget(index, "dead_1");
        AddAndForget(index, "dead_2");

        Collect();

        index.Prune().ShouldBe(2);
        index.Count.ShouldBe(1);
        index.TryGet("live", out _).ShouldBeTrue();
        GC.KeepAlive(kept);
    }
}
