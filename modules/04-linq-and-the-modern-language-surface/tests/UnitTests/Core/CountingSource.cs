namespace Training.Module04.Tests.Core;

/// <summary>
/// A sequence that records how many times it has been enumerated and how many
/// items were pulled. Multiple enumeration is invisible in results and obvious
/// here, which is the only way to assert on it.
/// </summary>
public sealed class CountingSource<T>(IEnumerable<T> items) : IEnumerable<T>
{
    public int Enumerations { get; private set; }

    public int ItemsPulled { get; private set; }

    public IEnumerator<T> GetEnumerator()
    {
        Enumerations++;

        foreach (var item in items)
        {
            ItemsPulled++;
            yield return item;
        }
    }

    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        => GetEnumerator();
}
