using Shouldly;
using Training.Module04.Challenge;

namespace Training.Module04.Tests.Challenge;

public sealed class InvoiceProjectionTests
{
    [Fact]
    public void An_invoice_with_no_lines_is_described_as_empty()
    {
        InvoiceProjection.Describe([]).ShouldBe("empty");
    }

    [Fact]
    public void A_single_line_is_named_directly()
    {
        InvoiceProjection.Describe([new LineItem("widget", 1, 10m)]).ShouldBe("widget only");
    }

    [Fact]
    public void Two_lines_are_named_as_a_pair()
    {
        InvoiceProjection.Describe([new LineItem("widget", 1, 10m), new LineItem("gadget", 1, 5m)])
            .ShouldBe("widget and gadget");
    }

    [Fact]
    public void More_than_two_lines_names_the_first_and_counts_the_rest()
    {
        LineItem[] lines =
        [
            new("widget", 1, 10m),
            new("gadget", 1, 5m),
            new("doodad", 1, 2m),
            new("thing", 1, 1m),
        ];

        InvoiceProjection.Describe(lines).ShouldBe("widget and 3 more");
    }

    [Fact]
    public void Totals_are_computed_from_quantity_and_unit_price()
    {
        LineItem[] lines = [new("widget", 3, 10m), new("gadget", 2, 5m)];

        InvoiceProjection.Total(lines).ShouldBe(40m);
    }

    [Fact]
    public void Applying_a_discount_leaves_the_original_untouched()
    {
        // `with` produces a copy. If the original changed, every caller holding
        // a reference to it just had their data rewritten underneath them.
        var line = new LineItem("widget", 2, 50m);

        var discounted = InvoiceProjection.Discounted(line, 0.10m);

        discounted.UnitPrice.ShouldBe(45m);
        line.UnitPrice.ShouldBe(50m);
    }

    [Fact]
    public void A_discount_keeps_everything_it_was_not_asked_to_change()
    {
        var line = new LineItem("widget", 7, 50m);

        var discounted = InvoiceProjection.Discounted(line, 0.20m);

        discounted.Sku.ShouldBe("widget");
        discounted.Quantity.ShouldBe(7);
    }
}
