namespace Training.Module01.Challenge;

/// <summary>
/// A table of canonical string instances.
///
/// This is what string.Intern does, minus the part that makes it dangerous:
/// entries here die with the table, whereas the runtime intern pool lives for
/// the life of the process and is never collected.
/// </summary>
public sealed class SymbolTable
{
    private readonly Dictionary<string, string> _symbols = new(StringComparer.Ordinal);

    public int Count => _symbols.Count;

    public string Intern(string value)
    {
        if (_symbols.TryGetValue(value, out var existing))
        {
            return existing;
        }

        _symbols[value] = value;
        return value;
    }
}
