using Shouldly;
using Training.Module01.Challenge;

namespace Training.Module01.Tests.Challenge;

public sealed class SymbolTableTests
{
    private static string NotInterned(string value) => new([.. value]);

    [Fact]
    public void Returns_the_same_instance_for_equal_strings()
    {
        var table = new SymbolTable();

        var first = table.Intern("USD");
        var second = table.Intern(NotInterned("USD"));

        ReferenceEquals(first, second).ShouldBeTrue();
    }

    [Fact]
    public void Reference_equality_is_therefore_safe_for_interned_symbols()
    {
        var table = new SymbolTable();

        var currencies = new[] { table.Intern("USD"), table.Intern(NotInterned("USD")) };

        (currencies[0] == currencies[1]).ShouldBeTrue();
        ReferenceEquals(currencies[0], currencies[1]).ShouldBeTrue();
    }

    [Fact]
    public void Does_not_grow_when_the_same_symbol_arrives_again()
    {
        var table = new SymbolTable();

        table.Intern("USD");
        table.Intern(NotInterned("USD"));
        table.Intern("EUR");

        table.Count.ShouldBe(2);
    }

    [Fact]
    public void Distinguishes_case_because_currency_codes_are_case_sensitive()
    {
        var table = new SymbolTable();

        table.Intern("USD");
        table.Intern("usd");

        table.Count.ShouldBe(2);
    }
}
