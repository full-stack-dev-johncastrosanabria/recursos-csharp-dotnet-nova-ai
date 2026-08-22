namespace Training.Module03.Core;

/// <summary>
/// Enriches a batch of orders by calling a per-order service.
///
/// Exercise: run the calls concurrently and return the results in input order.
/// A foreach that awaits each call in turn passes an "every input produces a
/// result" test perfectly, and is as slow as the sum of its parts. The test
/// that tells them apart watches how many calls are in flight at once.
/// </summary>
public static class OrderPipeline
{
    public static Task<IReadOnlyList<string>> EnrichAllAsync(
        IReadOnlyList<string> orderIds,
        Func<string, Task<string>> enrich)
        => throw new NotImplementedException();
}
