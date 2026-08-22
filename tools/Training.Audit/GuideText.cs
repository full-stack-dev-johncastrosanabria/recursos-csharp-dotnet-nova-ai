using System.Text.RegularExpressions;

namespace Training.Audit;

/// <summary>
/// Reading a guide the way the word-count gate needs to see it.
///
/// The count is over PROSE only. Counting raw words would let a guide reach
/// 3,000 on code listings and tables, which is precisely the failure the gate
/// exists to catch — a module that stopped explaining early and padded.
/// </summary>
public static partial class GuideText
{
    public static int CountProseWords(string markdown)
    {
        var prose = new List<string>();
        var inFence = false;

        foreach (var raw in markdown.Split('\n'))
        {
            var line = raw.TrimEnd('\r');
            var trimmed = line.TrimStart();

            if (trimmed.StartsWith("```", StringComparison.Ordinal)
                || trimmed.StartsWith("~~~", StringComparison.Ordinal))
            {
                inFence = !inFence;
                continue;
            }

            if (inFence
                || trimmed.Length == 0
                || trimmed.StartsWith('#')
                || trimmed.StartsWith('|')
                || trimmed.StartsWith("---", StringComparison.Ordinal))
            {
                continue;
            }

            prose.Add(InlineCode().Replace(line, " "));
        }

        return string.Join(' ', prose)
            .Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries)
            .Length;
    }

    /// <summary>
    /// Prose word count for each level-two section, keyed by heading text in
    /// document order and counted with the same rules as CountProseWords
    /// (fenced code, tables, and heading lines excluded) so a section's count
    /// and the document total never disagree about what counts as prose.
    /// Text before the first level-two heading is not attributed to any
    /// section and is dropped, matching SectionHeadings' view of the document.
    /// </summary>
    public static IReadOnlyDictionary<string, int> ProseWordsBySection(string markdown)
    {
        var counts = new Dictionary<string, int>(StringComparer.Ordinal);
        string? current = null;
        var inFence = false;

        foreach (var raw in markdown.Split('\n'))
        {
            var line = raw.TrimEnd('\r');
            var trimmed = line.TrimStart();

            if (trimmed.StartsWith("```", StringComparison.Ordinal)
                || trimmed.StartsWith("~~~", StringComparison.Ordinal))
            {
                inFence = !inFence;
                continue;
            }

            if (inFence)
            {
                continue;
            }

            if (trimmed.StartsWith("## ", StringComparison.Ordinal))
            {
                current = trimmed[3..].Trim();
                counts.TryAdd(current, 0);
                continue;
            }

            if (current is null
                || trimmed.Length == 0
                || trimmed.StartsWith('#')
                || trimmed.StartsWith('|')
                || trimmed.StartsWith("---", StringComparison.Ordinal))
            {
                continue;
            }

            var words = InlineCode().Replace(line, " ")
                .Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries)
                .Length;
            counts[current] += words;
        }

        return counts;
    }

    /// <summary>The level-two headings, in document order.</summary>
    public static IReadOnlyList<string> SectionHeadings(string markdown)
    {
        var headings = new List<string>();
        var inFence = false;

        foreach (var raw in markdown.Split('\n'))
        {
            var trimmed = raw.TrimEnd('\r').TrimStart();

            if (trimmed.StartsWith("```", StringComparison.Ordinal)
                || trimmed.StartsWith("~~~", StringComparison.Ordinal))
            {
                inFence = !inFence;
                continue;
            }

            if (!inFence && trimmed.StartsWith("## ", StringComparison.Ordinal))
            {
                headings.Add(trimmed[3..].Trim());
            }
        }

        return headings;
    }

    [GeneratedRegex("`[^`]*`")]
    private static partial Regex InlineCode();
}
