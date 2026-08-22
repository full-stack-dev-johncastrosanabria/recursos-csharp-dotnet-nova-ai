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
/// Bind does the recursive work, but it is worth knowing what it is matching.
/// Configuration is flat: a JSON array becomes "Carriers:0:Name",
/// "Carriers:1:Name", and an environment variable writes the same shape with
/// double underscores. That is what lets a single array element be overridden
/// from the environment -- and also why a gap in the indexes silently truncates
/// the array, since binding stops at the first missing index.
///
/// The quiet failure is the empty section. Binding something that does not
/// exist does not throw and does not return null; it returns a fully
/// constructed object with empty collections. The service starts, reports
/// healthy, and routes nothing. If a section is required, validate it -- an
/// empty collection is the shape a typo in a section name takes.
/// </summary>
public static class RoutingTableBinder
{
    public static RoutingOptions Bind(IConfiguration configuration)
    {
        var routing = new RoutingOptions();
        configuration.GetSection(RoutingOptions.SectionName).Bind(routing);

        return routing;
    }
}
