using Shouldly;
using Training.Module04.Core;

namespace Training.Module04.Tests.Core;

public sealed class PagedResultsTests
{
    private static readonly int[] Numbers = Enumerable.Range(1, 25).ToArray();

    [Fact]
    public void Returns_the_requested_page()
    {
        var page = PagedResults.Create(Numbers, pageNumber: 2, pageSize: 10);

        page.Items.ShouldBe([11, 12, 13, 14, 15, 16, 17, 18, 19, 20]);
    }

    [Fact]
    public void Reports_the_total_across_all_pages()
    {
        PagedResults.Create(Numbers, pageNumber: 1, pageSize: 10).TotalCount.ShouldBe(25);
    }

    [Fact]
    public void Computes_the_page_count_including_the_partial_last_page()
    {
        PagedResults.Create(Numbers, pageNumber: 1, pageSize: 10).TotalPages.ShouldBe(3);
    }

    [Fact]
    public void The_last_page_may_be_short()
    {
        PagedResults.Create(Numbers, pageNumber: 3, pageSize: 10).Items.ShouldBe([21, 22, 23, 24, 25]);
    }

    [Fact]
    public void A_page_past_the_end_is_empty_rather_than_an_error()
    {
        var page = PagedResults.Create(Numbers, pageNumber: 99, pageSize: 10);

        page.Items.ShouldBeEmpty();
        page.TotalCount.ShouldBe(25);
    }

    [Fact]
    public void The_source_is_enumerated_once_despite_needing_both_a_count_and_a_slice()
    {
        // The obvious implementation calls Count() and then Skip().Take(),
        // which walks the source twice. Over a database query that is two
        // round trips; over a stream the second walk may see different data.
        var source = new CountingSource<int>(Numbers);

        PagedResults.Create(source, pageNumber: 2, pageSize: 10);

        source.Enumerations.ShouldBe(1);
    }

    [Fact]
    public void A_page_size_below_one_is_refused()
    {
        Should.Throw<ArgumentOutOfRangeException>(() => PagedResults.Create(Numbers, 1, 0));
    }
}
