using Microsoft.Extensions.Configuration;

namespace Training.Module05.Challenge;

/// <summary>
/// Flattens configuration into something safe to log.
///
/// The recursion follows the shape configuration actually has: a node either
/// carries a value or contains children, and only the leaves are worth
/// printing. Emitting the intermediate sections as empty entries is what makes
/// naive dumps unreadable.
///
/// The redaction list is deliberately about the *key*, not the value. You
/// cannot recognise a secret by looking at it -- an API key and an account id
/// are both opaque strings -- but the person who named the setting already told
/// you what it holds. That also means this fails safe in one direction only: a
/// secret stored under an innocuous name still leaks, so treat this as one
/// layer, not as permission to log everything.
/// </summary>
public static class SecretRedaction
{
    private static readonly string[] SecretMarkers =
        ["key", "secret", "password", "token", "connectionstring"];

    public static IReadOnlyDictionary<string, string> Dump(IConfiguration configuration)
    {
        var dump = new Dictionary<string, string>(StringComparer.Ordinal);
        Collect(configuration, dump);

        return dump;
    }

    private static void Collect(IConfiguration section, Dictionary<string, string> dump)
    {
        foreach (var child in section.GetChildren())
        {
            if (child.Value is not null)
            {
                dump[child.Path] = LooksLikeASecret(child.Key) ? "***" : child.Value;
            }

            Collect(child, dump);
        }
    }

    private static bool LooksLikeASecret(string key)
        => SecretMarkers.Any(marker => key.Contains(marker, StringComparison.OrdinalIgnoreCase));
}
