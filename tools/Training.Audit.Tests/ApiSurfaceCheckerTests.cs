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
    public void Keeps_nested_types_with_the_same_name_in_separate_buckets()
    {
        const string content = """
            namespace Training.Demo.Core;

            public sealed class OuterA
            {
                public sealed class Enumerator
                {
                    public int Current => 1;
                }
            }

            public sealed class OuterB
            {
                public sealed class Enumerator
                {
                    public int Current => 2;
                }
            }
            """;

        WriteSource("Exercises", "Outers.cs", content);
        WriteSource("Solutions", "Outers.cs", content);

        ApiSurfaceChecker.Run(_root).ShouldBeEmpty();
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

    public void Dispose() => Directory.Delete(_root, recursive: true);
}
