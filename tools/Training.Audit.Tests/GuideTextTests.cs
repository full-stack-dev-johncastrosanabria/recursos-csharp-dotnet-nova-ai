using Shouldly;

namespace Training.Audit.Tests;

public sealed class GuideTextTests
{
    [Fact]
    public void Counts_plain_prose()
    {
        GuideText.CountProseWords("one two three four five").ShouldBe(5);
    }

    [Fact]
    public void Excludes_fenced_code_blocks()
    {
        var markdown = """
            one two three

            ```csharp
            public static void Main() { Console.WriteLine("this does not count"); }
            ```

            four five
            """;

        GuideText.CountProseWords(markdown).ShouldBe(5);
    }

    [Fact]
    public void Excludes_tables()
    {
        var markdown = """
            one two

            | Column | Other |
            |---|---|
            | value | value |

            three
            """;

        GuideText.CountProseWords(markdown).ShouldBe(3);
    }

    [Fact]
    public void Excludes_headings_but_keeps_the_prose_after_them()
    {
        GuideText.CountProseWords("## A heading here\n\nreal prose words").ShouldBe(3);
    }

    [Fact]
    public void Reads_level_two_headings_in_order()
    {
        var markdown = "# Title\n\n## First\n\n### Nested\n\n## Second\n";

        GuideText.SectionHeadings(markdown).ShouldBe(["First", "Second"]);
    }
}
