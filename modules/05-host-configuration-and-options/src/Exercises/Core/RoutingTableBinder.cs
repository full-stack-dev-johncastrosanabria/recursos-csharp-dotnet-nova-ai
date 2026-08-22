using Microsoft.Extensions.Configuration;

namespace Training.Module05.Core;

public sealed class CarrierOptions
{
    public string Name { get; set; } = "";

    public IList<string> Regions { get; set; } = [];
}

public sealed class RoutingOptions
{
    public const string SectionName = "Routing";

    public string DefaultRegion { get; set; } = "";

    public IList<CarrierOptions> Carriers { get; set; } = [];

    public IDictionary<string, int> Limits { get; set; } = new Dictionary<string, int>();
}

/// <summary>
/// Binds the routing section onto a typed object.
///
/// Exercise: bind scalars, an array of nested objects, arrays inside those, and
/// a dictionary section keyed by its child names.
///
/// The quietest bug in configuration lives here. Binding a section that does
/// not exist does not fail and does not return null — it returns a
/// fully-constructed object with empty collections, so the service starts
/// happily and routes nothing.
/// </summary>
public static class RoutingTableBinder
{
    public static RoutingOptions Bind(IConfiguration configuration)
        => throw new NotImplementedException();
}
