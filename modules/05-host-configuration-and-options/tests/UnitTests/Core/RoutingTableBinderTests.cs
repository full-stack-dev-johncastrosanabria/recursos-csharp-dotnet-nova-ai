using Microsoft.Extensions.Configuration;
using Shouldly;
using Training.Module05.Core;

namespace Training.Module05.Tests.Core;

public sealed class RoutingTableBinderTests
{
    private static IConfiguration Configuration(Dictionary<string, string?> settings)
        => new ConfigurationBuilder().AddInMemoryCollection(settings).Build();

    private static readonly Dictionary<string, string?> Settings = new()
    {
        ["Routing:DefaultRegion"] = "EU",
        ["Routing:Carriers:0:Name"] = "swift",
        ["Routing:Carriers:0:Regions:0"] = "EU",
        ["Routing:Carriers:0:Regions:1"] = "UK",
        ["Routing:Carriers:1:Name"] = "transglobal",
        ["Routing:Carriers:1:Regions:0"] = "US",
        ["Routing:Limits:maxParcelsPerBatch"] = "40",
        ["Routing:Limits:maxWeightKg"] = "12",
    };

    [Fact]
    public void Binds_a_scalar_from_the_section()
    {
        RoutingTableBinder.Bind(Configuration(Settings)).DefaultRegion.ShouldBe("EU");
    }

    [Fact]
    public void Binds_an_array_of_nested_objects()
    {
        // Arrays bind by index key, so "Carriers:0:Name" is how the provider
        // sees what a JSON file wrote as a list. Environment variables use the
        // same shape with __ separators, which is why an array element can be
        // overridden individually from the environment.
        var routing = RoutingTableBinder.Bind(Configuration(Settings));

        routing.Carriers.Count.ShouldBe(2);
        routing.Carriers[0].Name.ShouldBe("swift");
        routing.Carriers[1].Name.ShouldBe("transglobal");
    }

    [Fact]
    public void Binds_arrays_nested_inside_those_objects()
    {
        var routing = RoutingTableBinder.Bind(Configuration(Settings));

        routing.Carriers[0].Regions.ShouldBe(["EU", "UK"]);
        routing.Carriers[1].Regions.ShouldBe(["US"]);
    }

    [Fact]
    public void Binds_a_dictionary_section_keyed_by_its_child_names()
    {
        var routing = RoutingTableBinder.Bind(Configuration(Settings));

        routing.Limits["maxParcelsPerBatch"].ShouldBe(40);
        routing.Limits["maxWeightKg"].ShouldBe(12);
    }

    [Fact]
    public void An_absent_section_binds_to_defaults_rather_than_null()
    {
        // The quietest configuration bug there is. Bind a section that does not
        // exist and you get a fully-constructed object with empty collections,
        // so the service starts happily and routes nothing.
        var routing = RoutingTableBinder.Bind(Configuration([]));

        routing.ShouldNotBeNull();
        routing.Carriers.ShouldBeEmpty();
        routing.Limits.ShouldBeEmpty();
    }

    [Fact]
    public void Array_indexes_are_positions_rather_than_stable_identities()
    {
        // A gap neither truncates the list nor leaves a hole. The binder walks
        // the section's children in key order and appends each one, so the
        // element written at index 2 lands at index 1.
        //
        // The consequence is the one that bites: overriding "Carriers:2" from
        // the environment when the file already defines two carriers appends a
        // third instead of replacing anything, and every index after an
        // inserted element silently shifts. Configuration arrays are a poor
        // place to keep things you intend to address individually.
        var sparse = new Dictionary<string, string?>
        {
            ["Routing:Carriers:0:Name"] = "swift",
            ["Routing:Carriers:2:Name"] = "transglobal",
        };

        var carriers = RoutingTableBinder.Bind(Configuration(sparse)).Carriers;

        carriers.Count.ShouldBe(2);
        carriers[1].Name.ShouldBe("transglobal");
    }
}
