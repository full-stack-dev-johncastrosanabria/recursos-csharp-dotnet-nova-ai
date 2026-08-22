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

    [Fact]
    public void Ignores_a_heading_looking_line_inside_a_tilde_fence()
    {
        var markdown = """
            ## First

            ~~~
            ## Not a real section
            ~~~

            ## Second
            """;

        GuideText.SectionHeadings(markdown).ShouldBe(["First", "Second"]);
    }

    [Fact]
    public void Counts_prose_words_per_section()
    {
        var markdown = """
            ## First

            one two three

            ## Second

            four five
            """;

        var counts = GuideText.ProseWordsBySection(markdown);

        counts["First"].ShouldBe(3);
        counts["Second"].ShouldBe(2);
    }

    [Fact]
    public void Reports_zero_for_a_section_with_no_prose_under_it()
    {
        var markdown = "## Empty\n\n## Next\n\nsome words here\n";

        GuideText.ProseWordsBySection(markdown)["Empty"].ShouldBe(0);
    }

    [Fact]
    public void Excludes_fenced_code_and_tables_from_a_sections_count()
    {
        var markdown = """
            ## First

            one two

            ```csharp
            this does not count either
            ```

            | Column |
            |---|
            | value |

            three
            """;

        GuideText.ProseWordsBySection(markdown)["First"].ShouldBe(3);
    }

    [Fact]
    public void Attributes_nothing_to_text_before_the_first_heading()
    {
        var markdown = "stray prose with no heading yet\n\n## First\n\nreal content\n";

        var counts = GuideText.ProseWordsBySection(markdown);

        counts.ShouldContainKey("First");
        counts.Values.Sum().ShouldBe(2);
    }
}
