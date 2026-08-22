namespace Training.Module01.Challenge;

/// <summary>
/// A table of canonical string instances, so that hot-path comparisons can use
/// reference equality instead of character-by-character comparison.
///
/// Challenge: Intern must return the same instance every time it is given an
/// equal string, and Count must not grow when a symbol arrives twice. Do not
/// use string.Intern — that puts entries in a runtime-wide table that is never
/// collected, which is a memory leak with extra steps.
/// </summary>
public sealed class SymbolTable
{
    public int Count => throw new NotImplementedException();

    public string Intern(string value) => throw new NotImplementedException();
}
