namespace Training.Module03.Core;

/// <summary>
/// Enriches a batch of orders by calling a per-order service.
///
/// The loop starts every call before anything is awaited; `Task.WhenAll` then
/// observes them all. Moving the `await` inside the loop compiles, passes a
/// "every input produces a result" test, and takes the sum of the latencies
/// instead of the maximum. It also changes behaviour on failure: a sequential
/// loop stops at the first one, so the later calls never happen at all.
///
/// WhenAll returns results in the order the tasks were given, not the order
/// they finished, which is why no extra bookkeeping is needed here.
/// </summary>
public static class OrderPipeline
{
    public static async Task<IReadOnlyList<string>> EnrichAllAsync(
        IReadOnlyList<string> orderIds,
        Func<string, Task<string>> enrich)
    {
        var pending = new Task<string>[orderIds.Count];

        for (var i = 0; i < orderIds.Count; i++)
        {
            pending[i] = enrich(orderIds[i]);
        }

        return await Task.WhenAll(pending);
    }
}
