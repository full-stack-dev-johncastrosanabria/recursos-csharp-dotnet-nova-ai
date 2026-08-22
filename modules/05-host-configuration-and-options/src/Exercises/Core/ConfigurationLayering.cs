using Microsoft.Extensions.Configuration;

namespace Training.Module05.Core;

/// <summary>
/// Builds a configuration from layers.
///
/// Exercise: register the layers so that later ones win. Precedence is
/// registration order, and getting it backwards is not a subtle bug — it means
/// production reads a developer's local settings. It also looks entirely
/// correct in every environment where the layers happen to agree, which is most
/// of them, most of the time.
/// </summary>
public static class ConfigurationLayering
{
    public static IConfigurationRoot Build(
        IReadOnlyDictionary<string, string?> baseSettings,
        IReadOnlyDictionary<string, string?> environmentSettings,
        IReadOnlyDictionary<string, string?> processSettings)
        => throw new NotImplementedException();
}
