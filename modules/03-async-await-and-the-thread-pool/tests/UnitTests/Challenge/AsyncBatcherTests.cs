using Shouldly;
using Training.Module03.Challenge;

namespace Training.Module03.Tests.Challenge;

public sealed class AsyncBatcherTests
{
    private static async IAsyncEnumerable<int> Range(int count, Action<int>? onYield = null)
    {
        for (var i = 1; i <= count; i++)
        {
            onYield?.Invoke(i);
            await Task.Yield();
            yield return i;
        }
    }

    [Fact]
    public async Task Splits_a_stream_into_full_batches()
    {
        var batches = new List<int[]>();

        await foreach (var batch in AsyncBatcher.BatchAsync(Range(6), 3, CancellationToken.None))
        {
            batches.Add([.. batch]);
        }

        batches.Count.ShouldBe(2);
        batches[0].ShouldBe([1, 2, 3]);
        batches[1].ShouldBe([4, 5, 6]);
    }

    [Fact]
    public async Task The_last_partial_batch_is_still_delivered()
    {
        // Dropping the remainder is the bug people ship here, and it only shows
        // up when the item count stops being a multiple of the batch size.
        var batches = new List<int[]>();

        await foreach (var batch in AsyncBatcher.BatchAsync(Range(7), 3, CancellationToken.None))
        {
            batches.Add([.. batch]);
        }

        batches.Count.ShouldBe(3);
        batches[2].ShouldBe([7]);
    }

    [Fact]
    public async Task An_empty_stream_produces_no_batches()
    {
        var batches = 0;

        await foreach (var _ in AsyncBatcher.BatchAsync(Range(0), 3, CancellationToken.None))
        {
            batches++;
        }

        batches.ShouldBe(0);
    }

    [Fact]
    public async Task It_stays_lazy_and_does_not_drain_the_source_first()
    {
        // The whole reason to batch a stream is that the stream does not fit in
        // memory. An implementation that materialises the source to slice it
        // has thrown away the only property that mattered.
        var produced = 0;
        var batches = 0;

        await foreach (var _ in AsyncBatcher.BatchAsync(Range(100, _ => produced++), 5, CancellationToken.None))
        {
            batches++;
            if (batches == 2)
            {
                break;
            }
        }

        produced.ShouldBeLessThan(20);
    }

    [Fact]
    public async Task Cancellation_stops_the_enumeration()
    {
        using var cts = new CancellationTokenSource();
        var batches = 0;

        await Should.ThrowAsync<OperationCanceledException>(async () =>
        {
            await foreach (var _ in AsyncBatcher.BatchAsync(Range(100), 5, cts.Token))
            {
                batches++;
                if (batches == 2)
                {
                    await cts.CancelAsync();
                }
            }
        });

        batches.ShouldBe(2);
    }

    [Fact]
    public async Task A_batch_size_below_one_is_refused()
    {
        await Should.ThrowAsync<ArgumentOutOfRangeException>(async () =>
        {
            await foreach (var _ in AsyncBatcher.BatchAsync(Range(3), 0, CancellationToken.None))
            {
                // The exception is expected before the first batch arrives.
            }
        });
    }
}
