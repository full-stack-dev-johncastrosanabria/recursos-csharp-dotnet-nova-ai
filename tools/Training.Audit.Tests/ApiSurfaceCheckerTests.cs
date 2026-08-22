using Shouldly;

namespace Training.Audit.Tests;

public sealed class ApiSurfaceCheckerTests : IDisposable
{
    private readonly string _root = Directory.CreateTempSubdirectory("api-tests").FullName;

    private void WriteSource(string project, string fileName, string content)
    {
        var dir = Path.Combine(_root, "modules", "01-demo", "src", project, "Core");
        Directory.CreateDirectory(dir);
        File.WriteAllText(Path.Combine(dir, fileName), content);
    }

    private const string Stub = """
        namespace Training.Demo.Core;

        public sealed class Wallet
        {
            public decimal Balance(string currency) => throw new NotImplementedException();
        }
        """;

    [Fact]
    public void Reports_nothing_when_signatures_match()
    {
        WriteSource("Exercises", "Wallet.cs", Stub);
        WriteSource("Solutions", "Wallet.cs", """
            namespace Training.Demo.Core;

            public sealed class Wallet
            {
                public decimal Balance(string currency) => 0m;
            }
            """);

        ApiSurfaceChecker.Run(_root).ShouldBeEmpty();
    }

    [Fact]
    public void Reports_a_renamed_parameter()
    {
        WriteSource("Exercises", "Wallet.cs", Stub);
        WriteSource("Solutions", "Wallet.cs", """
            namespace Training.Demo.Core;

            public sealed class Wallet
            {
                public decimal Balance(string currencyCode) => 0m;
            }
            """);

        ApiSurfaceChecker.Run(_root).ShouldNotBeEmpty();
    }

    [Fact]
    public void Reports_a_nullability_difference()
    {
        WriteSource("Exercises", "Wallet.cs", Stub);
        WriteSource("Solutions", "Wallet.cs", """
            namespace Training.Demo.Core;

            public sealed class Wallet
            {
                public decimal Balance(string? currency) => 0m;
            }
            """);

        ApiSurfaceChecker.Run(_root).ShouldNotBeEmpty();
    }

    [Fact]
    public void Reports_an_extra_public_member_in_solutions()
    {
        WriteSource("Exercises", "Wallet.cs", Stub);
        WriteSource("Solutions", "Wallet.cs", """
            namespace Training.Demo.Core;

            public sealed class Wallet
            {
                public decimal Balance(string currency) => 0m;

                public void Reset() { }
            }
            """);

        var findings = ApiSurfaceChecker.Run(_root);

        findings.ShouldNotBeEmpty();
        findings[0].Message.ShouldContain("Reset");
    }

    [Fact]
    public void Ignores_private_members()
    {
        WriteSource("Exercises", "Wallet.cs", Stub);
        WriteSource("Solutions", "Wallet.cs", """
            namespace Training.Demo.Core;

            public sealed class Wallet
            {
                public decimal Balance(string currency) => Rate();

                private static decimal Rate() => 0m;
            }
            """);

        ApiSurfaceChecker.Run(_root).ShouldBeEmpty();
    }

    [Fact]
    public void Reports_a_static_modifier_difference()
    {
        WriteSource("Exercises", "Wallet.cs", Stub);
        WriteSource("Solutions", "Wallet.cs", """
            namespace Training.Demo.Core;

            public sealed class Wallet
            {
                public static decimal Balance(string currency) => 0m;
            }
            """);

        ApiSurfaceChecker.Run(_root).ShouldNotBeEmpty();
    }

    [Fact]
    public void Reports_a_renamed_positional_record_parameter()
    {
        WriteSource("Exercises", "Money.cs", """
            namespace Training.Demo.Core;

            public readonly record struct Money(decimal Amount, string Currency);
            """);
        WriteSource("Solutions", "Money.cs", """
            namespace Training.Demo.Core;

            public readonly record struct Money(decimal Amount, string CurrencyCode);
            """);

        ApiSurfaceChecker.Run(_root).ShouldNotBeEmpty();
    }

    [Fact]
    public void Reports_drift_between_same_named_nested_types_in_different_outers()
    {
        // Collapsing Outer.Enumerator and Outer2.Enumerator into one bare
        // "Enumerator" bucket would flatten both projects to the same
        // {MoveNext, Reset} set and hide the swap below. Qualifying by
        // enclosing type keeps them separate, so the swap is visible.
        WriteSource("Exercises", "Outers.cs", """
            namespace Training.Demo.Core;

            public sealed class Outer
            {
                public sealed class Enumerator
                {
                    public bool MoveNext() => false;
                }
            }

            public sealed class Outer2
            {
                public sealed class Enumerator
                {
                    public void Reset() { }
                }
            }
            """);
        WriteSource("Solutions", "Outers.cs", """
            namespace Training.Demo.Core;

            public sealed class Outer
            {
                public sealed class Enumerator
                {
                    public void Reset() { }
                }
            }

            public sealed class Outer2
            {
                public sealed class Enumerator
                {
                    public bool MoveNext() => false;
                }
            }
            """);

        var findings = ApiSurfaceChecker.Run(_root);

        findings.ShouldNotBeEmpty();
        findings.ShouldContain(f => f.Message.Contains("Outer.Enumerator"));
        findings.ShouldContain(f => f.Message.Contains("Outer2.Enumerator"));
    }

    [Fact]
    public void Ignores_a_signature_wrapped_across_lines()
    {
        WriteSource("Exercises", "Wallet.cs", """
            namespace Training.Demo.Core;

            public sealed class Wallet
            {
                public decimal Balance(
                    string currency) => throw new NotImplementedException();
            }
            """);
        WriteSource("Solutions", "Wallet.cs", """
            namespace Training.Demo.Core;

            public sealed class Wallet
            {
                public decimal Balance(string currency) => 0m;
            }
            """);

        ApiSurfaceChecker.Run(_root).ShouldBeEmpty();
    }

    [Fact]
    public void An_expression_bodied_property_matches_a_get_only_auto_property()
    {
        // `Start => throw ...` in a stub and `Start { get; }` in the solution are
        // the same public surface. A stub cannot use an auto-property when its
        // constructor has no body to assign it in, so this pairing is the normal
        // shape of a read-only value on a struct, not an edge case.
        WriteSource("Exercises", "Window.cs", """
            namespace Training.Demo.Core;

            public readonly struct Window
            {
                public int Start => throw new NotImplementedException();
            }
            """);
        WriteSource("Solutions", "Window.cs", """
            namespace Training.Demo.Core;

            public readonly struct Window
            {
                public int Start { get; }
            }
            """);

        ApiSurfaceChecker.Run(_root).ShouldBeEmpty();
    }

    [Fact]
    public void A_settable_property_still_differs_from_an_expression_bodied_one()
    {
        WriteSource("Exercises", "Window.cs", """
            namespace Training.Demo.Core;

            public sealed class Window
            {
                public int Start => throw new NotImplementedException();
            }
            """);
        WriteSource("Solutions", "Window.cs", """
            namespace Training.Demo.Core;

            public sealed class Window
            {
                public int Start { get; set; }
            }
            """);

        ApiSurfaceChecker.Run(_root).ShouldNotBeEmpty();
    }

    public void Dispose() => Directory.Delete(_root, recursive: true);
}
