using Shouldly;
using Training.Module04.Core;

namespace Training.Module04.Tests.Core;

public sealed class CustomerDeduplicationTests
{
    private static readonly Customer[] Customers =
    [
        new("ana@example.com", "Ana"),
        new("ANA@example.com", "Ana Ruiz"),
        new("bob@example.com", "Bob"),
        new("ana@Example.COM", "A. Ruiz"),
    ];

    [Fact]
    public void Collapses_addresses_that_differ_only_by_case()
    {
        // The upstream system does not normalise email case. Treating these as
        // three customers means three invoices to the same person.
        CustomerDeduplication.Unique(Customers).Count().ShouldBe(2);
    }

    [Fact]
    public void Keeps_the_first_occurrence_of_each_address()
    {
        var unique = CustomerDeduplication.Unique(Customers).ToArray();

        unique[0].DisplayName.ShouldBe("Ana");
        unique[1].DisplayName.ShouldBe("Bob");
    }

    [Fact]
    public void An_empty_source_produces_nothing()
    {
        CustomerDeduplication.Unique([]).ShouldBeEmpty();
    }

    [Fact]
    public void Deduplication_is_deferred_like_any_other_operator()
    {
        var source = new CountingSource<Customer>(Customers);

        var query = CustomerDeduplication.Unique(source);

        source.Enumerations.ShouldBe(0);
        _ = query.ToArray();
        source.Enumerations.ShouldBe(1);
    }

    [Fact]
    public void It_stays_lazy_and_stops_when_the_caller_stops()
    {
        // Taking two results must not drain a million-row source. An
        // implementation that builds a HashSet of everything before yielding
        // anything passes every other test here and fails this one.
        var source = new CountingSource<Customer>(Customers);

        _ = CustomerDeduplication.Unique(source).Take(1).ToArray();

        source.ItemsPulled.ShouldBe(1);
    }

    [Fact]
    public void Groups_customers_by_their_normalised_address()
    {
        var groups = CustomerDeduplication.GroupByAddress(Customers);

        groups["ana@example.com"].Count.ShouldBe(3);
        groups["bob@example.com"].Count.ShouldBe(1);
    }
}
