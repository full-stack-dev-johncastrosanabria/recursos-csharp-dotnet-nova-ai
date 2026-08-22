namespace Training.Module04.Core;

public sealed record Page<T>(IReadOnlyList<T> Items, int TotalCount, int TotalPages);

/// <summary>
/// Slices a sequence into pages.
///
/// One walk, on purpose. Count() followed by Skip().Take() reads well and
/// enumerates twice: two round trips against a database, and against a stream
/// two different views of the data, so the total and the page can disagree.
///
/// Materialising once is the honest trade here — this signature takes
/// IEnumerable, so there is no provider to push the paging into. When there is
/// one, page with the provider instead: see module 04's Challenge exercise and
/// module 11.
/// </summary>
public static class PagedResults
{
    public static Page<T> Create<T>(IEnumerable<T> source, int pageNumber, int pageSize)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(pageNumber, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(pageSize, 1);

        var all = source as IReadOnlyList<T> ?? [.. source];
        var totalPages = (all.Count + pageSize - 1) / pageSize;

        var items = all
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToArray();

        return new Page<T>(items, all.Count, totalPages);
    }
}
