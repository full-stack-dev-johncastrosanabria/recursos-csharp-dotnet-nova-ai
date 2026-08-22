using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Primitives;

namespace Training.Module05.Challenge;

/// <summary>
/// Watches one configuration key and tracks its value.
///
/// ChangeToken.OnChange is the whole exercise, and the reason it exists is that
/// a change token is single-use. It fires once and is then spent, so a hand-written
/// `configuration.GetReloadToken().RegisterChangeCallback(...)` is notified of
/// exactly one change and then never again. That is a particularly unpleasant
/// bug because the feature demonstrably works the first time you test it.
///
/// OnChange re-registers on every fire and hands back a subscription to dispose.
/// Disposing matters: the registration roots the callback, which roots this
/// object, which roots whatever it holds -- module 02's event-handler leak,
/// wearing configuration's clothes.
/// </summary>
public sealed class ConfigurationWatcher : IDisposable
{
    private readonly IConfigurationRoot _configuration;
    private readonly string _key;
    private readonly IDisposable? _subscription;
    private int _changes;
    private bool _disposed;

    public ConfigurationWatcher(IConfigurationRoot configuration, string key)
    {
        _configuration = configuration;
        _key = key;

        _subscription = ChangeToken.OnChange(
            configuration.GetReloadToken,
            () => Interlocked.Increment(ref _changes));
    }

    public string? Current => _configuration[_key];

    public int Changes => Volatile.Read(ref _changes);

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _subscription?.Dispose();
    }
}
