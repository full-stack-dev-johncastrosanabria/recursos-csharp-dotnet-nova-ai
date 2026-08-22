using Microsoft.Extensions.Configuration;

namespace Training.Module05.Challenge;

/// <summary>
/// Watches one configuration key and tracks its value.
///
/// Challenge: a change token fires once and is then spent. Subscribe by hand
/// and you are notified of exactly one change and then never again — which
/// looks like the feature working, because the first change does arrive.
/// Either re-register inside your own callback, or use ChangeToken.OnChange,
/// which exists to do that for you.
///
/// The subscription also roots the callback, which roots this object, which
/// roots whatever it holds. That is module 02's event-handler leak wearing
/// configuration's clothes, so disposal has to unregister — and be safe to
/// call twice.
/// </summary>
public sealed class ConfigurationWatcher : IDisposable
{
    public ConfigurationWatcher(IConfigurationRoot configuration, string key)
        => throw new NotImplementedException();

    public string? Current => throw new NotImplementedException();

    public int Changes => throw new NotImplementedException();

    public void Dispose() => throw new NotImplementedException();
}
