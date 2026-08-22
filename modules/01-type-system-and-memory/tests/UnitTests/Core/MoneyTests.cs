using Shouldly;
using Training.Module01.Core;

namespace Training.Module01.Tests.Core;

public sealed class MoneyTests
{
    [Fact]
    public void Two_amounts_in_the_same_currency_add()
    {
        var total = new Money(10.50m, "USD").Add(new Money(4.50m, "USD"));

        total.ShouldBe(new Money(15.00m, "USD"));
    }

    [Fact]
    public void Adding_across_currencies_is_refused_rather_than_guessed()
    {
        var usd = new Money(10m, "USD");

        Should.Throw<CurrencyMismatchException>(() => usd.Add(new Money(10m, "EUR")));
    }

    [Fact]
    public void Equal_values_are_equal_and_hash_the_same()
    {
        var left = new Money(10.00m, "USD").Add(new Money(9.99m, "USD"));
        var right = new Money(19.99m, "USD");

        left.ShouldBe(right);
        left.GetHashCode().ShouldBe(right.GetHashCode());
    }

    [Fact]
    public void The_same_amount_in_a_different_currency_is_a_different_value()
    {
        Money.Zero("USD").ShouldNotBe(Money.Zero("EUR"));
    }

    [Fact]
    public void Works_as_a_dictionary_key()
    {
        var runningTotal = Money.Zero("USD").Add(new Money(5m, "USD"));
        var ledger = new Dictionary<Money, int> { [runningTotal] = 1 };

        ledger[new Money(5m, "USD")].ShouldBe(1);
    }

    [Fact]
    public void Is_a_value_type_so_assignment_copies_instead_of_aliasing()
    {
        var original = new Money(10m, "USD");
        var copy = original;

        var scaled = copy.Multiply(3);

        typeof(Money).IsValueType.ShouldBeTrue();
        scaled.Amount.ShouldBe(30m);
        original.Amount.ShouldBe(10m);
    }

    [Fact]
    public void Zero_is_the_additive_identity_for_its_currency()
    {
        var price = new Money(7.25m, "USD");

        Money.Zero("USD").Add(price).ShouldBe(price);
    }

    [Fact]
    public void Multiplying_by_a_quantity_scales_the_amount_only()
    {
        new Money(3m, "USD").Multiply(4).ShouldBe(new Money(12m, "USD"));
    }
}
