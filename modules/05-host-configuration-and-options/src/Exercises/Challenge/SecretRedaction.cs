using Microsoft.Extensions.Configuration;

namespace Training.Module05.Challenge;

/// <summary>
/// Flattens configuration into something safe to log.
///
/// Challenge: a startup dump of effective configuration is genuinely useful --
/// it answers "what did this instance actually read" in one line. It is also
/// the most common way a production secret reaches a log aggregator, a support
/// ticket, and a screenshot in a chat channel.
///
/// Emit every leaf key with its full path. Replace the value with "***" when
/// the key looks like a secret: key, secret, password, token, or connection
/// string, matched case-insensitively anywhere in the key. Sections that only
/// contain other sections have no value of their own and must not appear.
/// </summary>
public static class SecretRedaction
{
    public static IReadOnlyDictionary<string, string> Dump(IConfiguration configuration)
        => throw new NotImplementedException();
}
