using Shouldly;
using Training.Module01.Core;

namespace Training.Module01.Tests.Core;

public sealed class CustomerReferenceTests
{
    [Fact]
    public void Two_references_with_the_same_parts_are_equal()
    {
        new CustomerReference("EU", 42).ShouldBe(new CustomerReference("EU", 42));
    }

    [Fact]
    public void Region_comparison_ignores_case_because_the_source_system_does()
    {
        new CustomerReference("eu", 42).ShouldBe(new CustomerReference("EU", 42));
    }

    [Fact]
    public void Equal_references_hash_the_same_or_hash_sets_break()
    {
        var set = new HashSet<CustomerReference> { new("EU", 42) };

        set.Contains(new CustomerReference("eu", 42)).ShouldBeTrue();
    }

    [Fact]
    public void Survives_a_dictionary_round_trip_with_a_different_instance()
    {
        var lookup = new Dictionary<CustomerReference, string> { [new("EU", 42)] = "Ana" };

        lookup[new CustomerReference("EU", 42)].ShouldBe("Ana");
    }

    [Fact]
    public void The_equality_operator_agrees_with_Equals()
    {
        var left = new CustomerReference("EU", 42);
        var right = new CustomerReference("EU", 42);

        (left == right).ShouldBeTrue();
        (left != right).ShouldBeFalse();
    }

    [Fact]
    public void Null_is_handled_without_throwing()
    {
        CustomerReference? nothing = null;

        (nothing == null).ShouldBeTrue();
        (new CustomerReference("EU", 42) == null).ShouldBeFalse();
        new CustomerReference("EU", 42).Equals(null).ShouldBeFalse();
    }

    [Fact]
    public void A_different_number_is_a_different_customer()
    {
        new CustomerReference("EU", 42).ShouldNotBe(new CustomerReference("EU", 43));
    }
}
