namespace Training.Module04.Core;

public sealed record Page<T>(IReadOnlyList<T> Items, int TotalCount, int TotalPages);

/// <summary>
/// Slices a sequence into pages.
///
/// Exercise: a page needs both a total and a slice, and the obvious way to get
/// them — Count() followed by Skip().Take() — walks the source twice. Against a
/// database that is two round trips; against a stream the second walk may see
/// different data than the first, so the total and the page disagree.
/// </summary>
public static class PagedResults
{
    public static Page<T> Create<T>(IEnumerable<T> source, int pageNumber, int pageSize)
        => throw new NotImplementedException();
}
