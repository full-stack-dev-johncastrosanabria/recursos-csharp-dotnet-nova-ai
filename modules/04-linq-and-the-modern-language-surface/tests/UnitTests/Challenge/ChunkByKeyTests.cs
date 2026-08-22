using Shouldly;
using Training.Module04.Challenge;

namespace Training.Module04.Tests.Challenge;

public sealed class ChunkByKeyTests
{
    private sealed class CountingSource<T>(IEnumerable<T> items) : IEnumerable<T>
    {
        public int ItemsPulled { get; private set; }

        public IEnumerator<T> GetEnumerator()
        {
            foreach (var item in items)
            {
                ItemsPulled++;
                yield return item;
            }
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
            => GetEnumerator();
    }

    private static readonly string[] Alternating = ["EU", "US", "EU"];
    private static readonly string[] Runs = ["EU", "EU", "US", "US", "US", "EU"];
    private static readonly string[] SingleRegion = ["EU"];
    private static readonly string[] TwoRegions = ["EU", "US"];
    private static readonly int[] Amounts = [1, 3, 5, 2, 4, 7];

    [Fact]
    public void Groups_runs_of_consecutive_equal_keys()
    {
        var regions = Runs;

        var chunks = ChunkByKey.Chunk(regions, r => r).Select(c => c.ToArray()).ToArray();

        chunks.Length.ShouldBe(3);
        chunks[0].ShouldBe(["EU", "EU"]);
        chunks[1].ShouldBe(["US", "US", "US"]);
        chunks[2].ShouldBe(["EU"]);
    }

    [Fact]
    public void Unlike_GroupBy_it_does_not_merge_runs_that_are_apart()
    {
        // GroupBy would produce two groups here, EU and US, and would buffer
        // the whole source to do it. Consecutive chunking is what a streaming
        // report needs -- and it is why this operator is not in the BCL.
        var regions = Alternating;

        ChunkByKey.Chunk(regions, r => r).Count().ShouldBe(3);
    }

    [Fact]
    public void An_empty_source_produces_no_chunks()
    {
        ChunkByKey.Chunk(Array.Empty<string>(), r => r).ShouldBeEmpty();
    }

    [Fact]
    public void A_single_item_produces_one_chunk()
    {
        var chunks = ChunkByKey.Chunk(SingleRegion, r => r).ToArray();

        chunks.Length.ShouldBe(1);
        chunks[0].ShouldBe(["EU"]);
    }

    [Fact]
    public void It_streams_rather_than_buffering_the_source()
    {
        // Taking the first chunk must not drain the source. GroupBy cannot do
        // this: it has to see every item before it can return any group.
        var source = new CountingSource<string>(Runs);

        _ = ChunkByKey.Chunk(source, r => r).Take(1).ToArray();

        source.ItemsPulled.ShouldBeLessThanOrEqualTo(3);
    }

    [Fact]
    public void Chunking_is_deferred_until_enumeration()
    {
        var source = new CountingSource<string>(TwoRegions);

        _ = ChunkByKey.Chunk(source, r => r);

        source.ItemsPulled.ShouldBe(0);
    }

    [Fact]
    public void The_key_selector_decides_what_counts_as_the_same_run()
    {
        var amounts = Amounts;

        var chunks = ChunkByKey.Chunk(amounts, n => n % 2).Select(c => c.ToArray()).ToArray();

        chunks.Length.ShouldBe(3);
        chunks[0].ShouldBe([1, 3, 5]);
        chunks[1].ShouldBe([2, 4]);
        chunks[2].ShouldBe([7]);
    }
}
