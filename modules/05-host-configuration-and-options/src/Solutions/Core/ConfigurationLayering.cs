using Microsoft.Extensions.Configuration;

namespace Training.Module05.Core;

/// <summary>
/// Builds a configuration from layers.
///
/// Precedence is registration order and the last one wins. That is the whole
/// rule, and it is worth stating out loud because reversing it produces a
/// system that behaves correctly in every environment where the layers agree --
/// which is most of them, most of the time, right up until production reads a
/// developer's local settings.
/// </summary>
public static class ConfigurationLayering
{
    public static IConfigurationRoot Build(
        IReadOnlyDictionary<string, string?> baseSettings,
        IReadOnlyDictionary<string, string?> environmentSettings,
        IReadOnlyDictionary<string, string?> processSettings)
        => new ConfigurationBuilder()
            .AddInMemoryCollection(baseSettings)
            .AddInMemoryCollection(environmentSettings)
            .AddInMemoryCollection(processSettings)
            .Build();
}
