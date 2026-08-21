# recursos-csharp-dotnet-nova-ai Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build a public 30-module C#/.NET training repository whose every published exercise is provably solvable, provably unsolved in the stubs, and provably documented to a fixed anatomy — all enforced by CI rather than by good intentions.

**Architecture:** Each module owns three projects — `src/Exercises` (stubs that throw), `src/Solutions` (reference implementations), `tests/UnitTests` — where the two source projects share identical namespaces and public signatures and differ only in assembly name. A single `Directory.Build.targets` at the repo root points every test project at one or the other based on the `UseSolutions` MSBuild property. A custom `Training.Audit` tool enforces the invariants that MSBuild cannot: file pairing, public API parity between the two projects, guide anatomy, and prose word count.

**Tech Stack:** .NET 10 LTS (`net10.0`, C# 14) · xunit.v3 4.0.0 on Microsoft Testing Platform · Shouldly 4.3.0 · NSubstitute 6.2.0 · Roslyn (`Microsoft.CodeAnalysis.CSharp` 5.9.0) for the API-parity check · Testcontainers 4.14.0 + PostgreSQL 18 for the integration tier · MkDocs Material.

**Spec:** `docs/superpowers/specs/2026-08-20-recursos-csharp-dotnet-nova-ai-design.md`

## Global Constraints

Every task's requirements implicitly include this section. Values are copied verbatim from the spec.

- **Target framework** `net10.0`, language **C# 14**. .NET 10 is the current LTS (EOL 2028-11-14). Do not target `net11.0` — it is preview.
- **`<Nullable>enable</Nullable>`** and **`<TreatWarningsAsErrors>true</TreatWarningsAsErrors>`** on every project without exception.
- **Analyser relaxation is limited to exactly four rules** — `IDE0060`, `CA1801`, `CA1822`, `CS1998` — and only inside `modules/*/src/Exercises/` via a nested `.editorconfig`. Nothing else is relaxed anywhere.
- **No package that is not free for commercial use.** Specifically banned: FluentAssertions 8+, MediatR 13+, AutoMapper 15+, MassTransit 9+. Verified-free replacements: Shouldly, a hand-rolled mediator, explicit mapping methods, MassTransit 8.5.4.
- **xunit.v3 runs on Microsoft Testing Platform.** Test projects are `<OutputType>Exe</OutputType>` with `<TestingPlatformDotnetTestSupport>true</TestingPlatformDotnetTestSupport>`. Do **not** add `Microsoft.NET.Test.Sdk` or `coverlet.collector`.
- **All package versions are centrally managed** in `Directory.Packages.props`. No `Version=` attribute on any `PackageReference`.
- **EF Core migrations are always explicit.** `EnsureCreated` must not appear anywhere in the repository, including examples.
- **Prose in English**; identifiers, folder names and commit messages in English.
- **Guides are 3,000–5,000 prose words** (fenced code, tables and front matter excluded from the count). Outside that range is a CI failure.
- **Real-world cases**: the failure must be reproducible by a program in the module's own `examples/`, and any cost figure must be arithmetic over named assumptions. No invented incidents, dates, companies or invoices.
- **Domain** is order-to-cash for a marketplace: orders, payments, a ledger, inventory reservations, shipments, notifications.

---

## File Structure

**Phase 0 — machinery** (no content decisions; everything here is testable on its own)

| Path | Responsibility |
|---|---|
| `global.json` | pins SDK band 10.0.2xx, `rollForward: latestFeature` |
| `Directory.Build.props` | framework, language, nullable, warnings-as-errors, analysers |
| `Directory.Build.targets` | the single `UseSolutions` conditional project reference |
| `Directory.Packages.props` | every package version, centrally |
| `.editorconfig` | analyser and style rules at severity=error |
| `tools/Training.Audit/` | the invariant checker — one file per check |
| `tools/Training.Audit.Tests/` | tests for the checker itself |
| `tools/Training.Scaffold/` | generates a module triple from templates |
| `tools/hooks/pre-commit`, `tools/install-hooks.sh` | pre-commit gate |
| `run.sh`, `run.ps1` | the learner's entry point: `test`, `status`, `reset` |
| `.github/workflows/{ci,docs,links}.yml` | the six CI jobs |
| `mkdocs.yml`, `requirements-docs.txt` | the site |
| `README.md`, `START-HERE.md`, `CONTRIBUTING.md`, `LICENSE`, `LICENSE-CONTENT` | front door |
| `modules/00-probe/` | throwaway module proving the machinery; deleted in Task 12 |

`Training.Audit` is split one-file-per-check so a subagent can hold any single check in context:

```
tools/Training.Audit/
├── Training.Audit.csproj
├── Program.cs            subcommand dispatch + exit codes
├── AuditFinding.cs       the one result type every check returns
├── RepoLayout.cs         path conventions in ONE place
├── PairChecker.cs        every test file has both counterparts
├── ApiSurfaceChecker.cs  Exercises and Solutions expose identical public API
├── GuideAnatomyChecker.cs required sections, order, prose word count
├── GuideText.cs          markdown → prose word count
├── TrxReport.cs          parse a TRX file
├── StubLeakChecker.cs    every exercise test class has ≥1 failure
└── StatusReporter.cs     per-module progress table
```

**Phase 1 — module 01** (the bar for the other 29)

```
modules/01-type-system-and-memory/
├── GUIDE.md
├── examples/{EqualitySurprises,BoxingCosts,BasketBug}/
├── src/Exercises/{Exercises.csproj, .editorconfig, Core/, Challenge/}
├── src/Solutions/{Solutions.csproj, Core/, Challenge/}
└── tests/UnitTests/{UnitTests.csproj, Core/, Challenge/}
```

---

## Task 1: Build foundations that fail loudly

**Files:**
- Create: `global.json`, `Directory.Build.props`, `Directory.Packages.props`, `.editorconfig`
- Create (temporary): `modules/00-probe/src/Exercises/Exercises.csproj`, `modules/00-probe/src/Exercises/Core/Probe.cs`

**Interfaces:**
- Consumes: nothing.
- Produces: the property set every later project inherits — `TargetFramework=net10.0`, `Nullable=enable`, `TreatWarningsAsErrors=true`, `ManagePackageVersionsCentrally=true`.

- [ ] **Step 1: Write the failing test — a project that must not build**

Create `global.json`:

```json
{
  "sdk": {
    "version": "10.0.200",
    "rollForward": "latestFeature"
  }
}
```

Create `Directory.Build.props`:

```xml
<Project>
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <LangVersion>14.0</LangVersion>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
    <EnforceCodeStyleInBuild>true</EnforceCodeStyleInBuild>
    <AnalysisMode>Recommended</AnalysisMode>
    <InvariantGlobalization>true</InvariantGlobalization>
    <GenerateDocumentationFile>false</GenerateDocumentationFile>
  </PropertyGroup>
</Project>
```

Create `Directory.Packages.props`:

```xml
<Project>
  <PropertyGroup>
    <ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>
    <CentralPackageTransitivePinningEnabled>true</CentralPackageTransitivePinningEnabled>
  </PropertyGroup>
  <ItemGroup>
    <PackageVersion Include="xunit.v3" Version="4.0.0" />
    <PackageVersion Include="Shouldly" Version="4.3.0" />
    <PackageVersion Include="NSubstitute" Version="6.2.0" />
    <PackageVersion Include="Microsoft.Testing.Extensions.TrxReport" Version="2.3.3" />
    <PackageVersion Include="Microsoft.CodeAnalysis.CSharp" Version="5.9.0" />
  </ItemGroup>
</Project>
```

Create `.editorconfig`:

```ini
root = true

[*]
end_of_line = lf
insert_final_newline = true
charset = utf-8
trim_trailing_whitespace = true

[*.{cs,csx}]
indent_style = space
indent_size = 4
csharp_style_namespace_declarations = file_scoped:error
csharp_style_var_when_type_is_apparent = true:suggestion
dotnet_style_require_accessibility_modifiers = for_non_interface_members:error
dotnet_diagnostic.IDE0005.severity = error
dotnet_diagnostic.IDE0055.severity = error
dotnet_diagnostic.CA1062.severity = none
dotnet_diagnostic.CA2007.severity = none

[*.{xml,csproj,props,targets,yml,yaml,json}]
indent_style = space
indent_size = 2

[*.md]
trim_trailing_whitespace = false
```

Create `modules/00-probe/src/Exercises/Exercises.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <RootNamespace>Training.Probe</RootNamespace>
    <AssemblyName>Exercises</AssemblyName>
  </PropertyGroup>
</Project>
```

Create `modules/00-probe/src/Exercises/Core/Probe.cs` with a deliberate violation — an unused private field, which trips `IDE0051`:

```csharp
namespace Training.Probe.Core;

public static class Probe
{
    private static readonly int Unused = 42;

    public static int Answer() => 42;
}
```

- [ ] **Step 2: Run the build to verify it fails**

Run: `dotnet build modules/00-probe/src/Exercises/Exercises.csproj`

Expected: **FAIL**, with an error (not a warning) mentioning `IDE0051` — the private field is never used. If this only warns, `TreatWarningsAsErrors` or `EnforceCodeStyleInBuild` is not being applied and the whole gate is decorative. Do not continue until this fails.

- [ ] **Step 3: Remove the violation**

Replace `modules/00-probe/src/Exercises/Core/Probe.cs` with:

```csharp
namespace Training.Probe.Core;

public static class Probe
{
    public static int Answer() => 42;
}
```

- [ ] **Step 4: Run the build to verify it passes**

Run: `dotnet build modules/00-probe/src/Exercises/Exercises.csproj`
Expected: **PASS**, `Build succeeded`, 0 warnings.

- [ ] **Step 5: Commit**

```bash
git add global.json Directory.Build.props Directory.Packages.props .editorconfig modules/00-probe
git commit -m "Add build foundations with warnings as errors

Proven by a probe project: an unused private field fails the build rather
than warning. Target is net10.0 (current LTS, EOL 2028-11-14) with C# 14."
```

---

## Task 2: The Exercises/Solutions swap

This is the heart of the repository. Everything else is scaffolding around it.

**Files:**
- Create: `Directory.Build.targets`
- Create: `modules/00-probe/src/Solutions/Solutions.csproj`, `modules/00-probe/src/Solutions/Core/Probe.cs`
- Create: `modules/00-probe/tests/UnitTests/UnitTests.csproj`, `modules/00-probe/tests/UnitTests/Core/ProbeTests.cs`
- Modify: `modules/00-probe/src/Exercises/Core/Probe.cs`

**Interfaces:**
- Consumes: the property set from Task 1.
- Produces: the MSBuild contract every module depends on — a test project that sets `<IsTrainingTestProject>true</IsTrainingTestProject>` receives a `ProjectReference` to `..\..\src\Exercises` by default and to `..\..\src\Solutions` when `UseSolutions=true`.

- [ ] **Step 1: Write the failing test**

Create `modules/00-probe/tests/UnitTests/Core/ProbeTests.cs`:

```csharp
using Shouldly;
using Training.Probe.Core;

namespace Training.Probe.Tests.Core;

public sealed class ProbeTests
{
    [Fact]
    public void Answer_returns_forty_two()
    {
        Probe.Answer().ShouldBe(42);
    }
}
```

Create `modules/00-probe/tests/UnitTests/UnitTests.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <RootNamespace>Training.Probe.Tests</RootNamespace>
    <AssemblyName>Probe.UnitTests</AssemblyName>
    <OutputType>Exe</OutputType>
    <TestingPlatformDotnetTestSupport>true</TestingPlatformDotnetTestSupport>
    <IsTrainingTestProject>true</IsTrainingTestProject>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="xunit.v3" />
    <PackageReference Include="Shouldly" />
    <PackageReference Include="Microsoft.Testing.Extensions.TrxReport" />
  </ItemGroup>

  <ItemGroup>
    <Using Include="Xunit" />
  </ItemGroup>
</Project>
```

Create `Directory.Build.targets` — the swap, written once for the whole repository:

```xml
<Project>

  <!--
    Every training test project references exactly one of its module's two
    source projects. Both expose identical namespaces and identical public
    signatures, so the test code compiles unchanged against either.

      dotnet test <module>                        -> Exercises  (red until solved)
      dotnet test <module> -p:UseSolutions=true   -> Solutions  (must be green)

    CI runs the second form to prove every published exercise is solvable.
  -->
  <ItemGroup Condition="'$(IsTrainingTestProject)' == 'true'">
    <ProjectReference Include="$(MSBuildProjectDirectory)\..\..\src\Exercises\Exercises.csproj"
                      Condition="'$(UseSolutions)' != 'true'" />
    <ProjectReference Include="$(MSBuildProjectDirectory)\..\..\src\Solutions\Solutions.csproj"
                      Condition="'$(UseSolutions)' == 'true'" />
  </ItemGroup>

</Project>
```

Turn the probe exercise into a real stub — `modules/00-probe/src/Exercises/Core/Probe.cs`:

```csharp
namespace Training.Probe.Core;

public static class Probe
{
    public static int Answer() => throw new NotImplementedException();
}
```

Create the solution — `modules/00-probe/src/Solutions/Core/Probe.cs`:

```csharp
namespace Training.Probe.Core;

public static class Probe
{
    public static int Answer() => 42;
}
```

Create `modules/00-probe/src/Solutions/Solutions.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <RootNamespace>Training.Probe</RootNamespace>
    <AssemblyName>Solutions</AssemblyName>
  </PropertyGroup>
</Project>
```

- [ ] **Step 2: Run the tests against the stubs — they must fail**

Run: `dotnet test modules/00-probe/tests/UnitTests`

Expected: **FAIL**, 1 test failed, the failure message containing `System.NotImplementedException`. A pass here means the stub already contains the answer.

- [ ] **Step 3: Run the tests against the solutions — they must pass**

Run: `dotnet test modules/00-probe/tests/UnitTests -p:UseSolutions=true`

Expected: **PASS**, 1 test passed. This is the invariant the whole repository rests on: same test code, same namespaces, different assembly.

- [ ] **Step 4: Verify the stub project still builds clean**

Run: `dotnet build modules/00-probe/src/Exercises/Exercises.csproj`

Expected: **PASS** with 0 warnings. A method whose body is `throw new NotImplementedException()` and which takes no parameters trips nothing yet — Task 13 is where the four-rule relaxation becomes necessary, once stubs take parameters.

- [ ] **Step 5: Commit**

```bash
git add Directory.Build.targets modules/00-probe
git commit -m "Add the Exercises/Solutions swap

One conditional ProjectReference in Directory.Build.targets serves every test
project in the repo. Proven both ways on the probe module: red against stubs,
green with -p:UseSolutions=true."
```

---

## Task 3: Audit tool skeleton and the pair check

**Files:**
- Create: `tools/Training.Audit/Training.Audit.csproj`, `AuditFinding.cs`, `RepoLayout.cs`, `PairChecker.cs`, `Program.cs`
- Create: `tools/Training.Audit.Tests/Training.Audit.Tests.csproj`, `PairCheckerTests.cs`
- Modify: `Directory.Packages.props`

**Interfaces:**
- Consumes: nothing from earlier tasks (this is a standalone console app).
- Produces:
  - `public sealed record AuditFinding(string Check, string Path, string Message)`
  - `public static class RepoLayout` with `IEnumerable<string> ModuleDirectories(string repoRoot)`, `string? ExerciseCounterpart(string repoRoot, string testFilePath)`, `string? SolutionCounterpart(string repoRoot, string testFilePath)`
  - `public static class PairChecker` with `IReadOnlyList<AuditFinding> Run(string repoRoot)`

- [ ] **Step 1: Write the failing test**

Create `tools/Training.Audit.Tests/PairCheckerTests.cs`:

```csharp
using Shouldly;
using Training.Audit;

namespace Training.Audit.Tests;

public sealed class PairCheckerTests : IDisposable
{
    private readonly string _root = Directory.CreateTempSubdirectory("audit-tests").FullName;

    private void WriteFile(string relativePath, string content = "")
    {
        var full = Path.Combine(_root, relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(full)!);
        File.WriteAllText(full, content);
    }

    [Fact]
    public void Reports_nothing_when_every_test_has_both_counterparts()
    {
        WriteFile("modules/01-demo/tests/UnitTests/Core/MoneyTests.cs");
        WriteFile("modules/01-demo/src/Exercises/Core/Money.cs");
        WriteFile("modules/01-demo/src/Solutions/Core/Money.cs");

        PairChecker.Run(_root).ShouldBeEmpty();
    }

    [Fact]
    public void Reports_the_missing_stub_when_only_the_solution_exists()
    {
        WriteFile("modules/01-demo/tests/UnitTests/Core/MoneyTests.cs");
        WriteFile("modules/01-demo/src/Solutions/Core/Money.cs");

        var findings = PairChecker.Run(_root);

        findings.Count.ShouldBe(1);
        findings[0].Message.ShouldContain("src/Exercises/Core/Money.cs");
    }

    [Fact]
    public void Reports_the_missing_solution_when_only_the_stub_exists()
    {
        WriteFile("modules/01-demo/tests/UnitTests/Core/MoneyTests.cs");
        WriteFile("modules/01-demo/src/Exercises/Core/Money.cs");

        var findings = PairChecker.Run(_root);

        findings.Count.ShouldBe(1);
        findings[0].Message.ShouldContain("src/Solutions/Core/Money.cs");
    }

    [Fact]
    public void Ignores_test_files_that_do_not_end_in_Tests()
    {
        WriteFile("modules/01-demo/tests/UnitTests/TestHelpers.cs");

        PairChecker.Run(_root).ShouldBeEmpty();
    }

    public void Dispose() => Directory.Delete(_root, recursive: true);
}
```

Create `tools/Training.Audit.Tests/Training.Audit.Tests.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <RootNamespace>Training.Audit.Tests</RootNamespace>
    <OutputType>Exe</OutputType>
    <TestingPlatformDotnetTestSupport>true</TestingPlatformDotnetTestSupport>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="xunit.v3" />
    <PackageReference Include="Shouldly" />
    <PackageReference Include="Microsoft.Testing.Extensions.TrxReport" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\Training.Audit\Training.Audit.csproj" />
  </ItemGroup>

  <ItemGroup>
    <Using Include="Xunit" />
  </ItemGroup>
</Project>
```

Note: this project deliberately does **not** set `IsTrainingTestProject`, so `Directory.Build.targets` leaves it alone.

- [ ] **Step 2: Run the test to verify it fails**

Run: `dotnet test tools/Training.Audit.Tests`
Expected: **FAIL** to compile — `Training.Audit` does not exist yet.

- [ ] **Step 3: Write the implementation**

Create `tools/Training.Audit/Training.Audit.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <RootNamespace>Training.Audit</RootNamespace>
    <OutputType>Exe</OutputType>
  </PropertyGroup>
</Project>
```

Create `tools/Training.Audit/AuditFinding.cs`:

```csharp
namespace Training.Audit;

/// <summary>One thing that is wrong with the repository.</summary>
/// <param name="Check">Which check produced this, e.g. "pairs".</param>
/// <param name="Path">Repo-relative path the finding is about.</param>
/// <param name="Message">What is wrong, in terms the author can act on.</param>
public sealed record AuditFinding(string Check, string Path, string Message);
```

Create `tools/Training.Audit/RepoLayout.cs` — every path convention lives here and nowhere else:

```csharp
namespace Training.Audit;

/// <summary>
/// The repository's path conventions, in one place. Every other check asks
/// this class where things live rather than composing paths itself.
/// </summary>
public static class RepoLayout
{
    public const string ModulesFolder = "modules";
    public const string TestSuffix = "Tests.cs";

    public static IEnumerable<string> ModuleDirectories(string repoRoot)
    {
        var modules = Path.Combine(repoRoot, ModulesFolder);
        return Directory.Exists(modules)
            ? Directory.EnumerateDirectories(modules).OrderBy(d => d, StringComparer.Ordinal)
            : [];
    }

    /// <summary>Every *Tests.cs under a module's tests/ folder.</summary>
    public static IEnumerable<string> TestFiles(string moduleDirectory)
    {
        var tests = Path.Combine(moduleDirectory, "tests");
        return Directory.Exists(tests)
            ? Directory.EnumerateFiles(tests, "*" + TestSuffix, SearchOption.AllDirectories)
                       .OrderBy(f => f, StringComparer.Ordinal)
            : [];
    }

    /// <summary>
    /// modules/01-x/tests/UnitTests/Core/MoneyTests.cs
    ///   -> modules/01-x/src/Exercises/Core/Money.cs
    /// Returns null when the path is not a recognisable test file.
    /// </summary>
    public static string? ExerciseCounterpart(string moduleDirectory, string testFilePath)
        => Counterpart(moduleDirectory, testFilePath, "Exercises");

    public static string? SolutionCounterpart(string moduleDirectory, string testFilePath)
        => Counterpart(moduleDirectory, testFilePath, "Solutions");

    private static string? Counterpart(string moduleDirectory, string testFilePath, string project)
    {
        var fileName = Path.GetFileName(testFilePath);
        if (!fileName.EndsWith(TestSuffix, StringComparison.Ordinal))
        {
            return null;
        }

        // Everything between the test project folder and the file itself is
        // preserved, so Core/ and Challenge/ map straight across.
        var testsRoot = Path.Combine(moduleDirectory, "tests");
        var relative = Path.GetRelativePath(testsRoot, testFilePath);
        var segments = relative.Split(Path.DirectorySeparatorChar);
        if (segments.Length < 2)
        {
            return null;
        }

        var subPath = Path.Combine(segments[1..^1]);
        var sourceName = fileName[..^TestSuffix.Length] + ".cs";

        return Path.Combine(moduleDirectory, "src", project, subPath, sourceName);
    }
}
```

Create `tools/Training.Audit/PairChecker.cs`:

```csharp
namespace Training.Audit;

/// <summary>
/// Every test file must have a stub in src/Exercises AND a reference
/// implementation in src/Solutions.
///
/// The solvability job already proves each solution satisfies its test. This
/// proves the other half: that the learner's stub exists at all. Without it, a
/// pull request that adds a test and a solution but forgets the stub goes green.
/// </summary>
public static class PairChecker
{
    public const string Name = "pairs";

    public static IReadOnlyList<AuditFinding> Run(string repoRoot)
    {
        var findings = new List<AuditFinding>();

        foreach (var module in RepoLayout.ModuleDirectories(repoRoot))
        {
            foreach (var testFile in RepoLayout.TestFiles(module))
            {
                foreach (var project in (string[])["Exercises", "Solutions"])
                {
                    var counterpart = project == "Exercises"
                        ? RepoLayout.ExerciseCounterpart(module, testFile)
                        : RepoLayout.SolutionCounterpart(module, testFile);

                    if (counterpart is null || File.Exists(counterpart))
                    {
                        continue;
                    }

                    findings.Add(new AuditFinding(
                        Name,
                        Relative(repoRoot, testFile),
                        $"has no counterpart at {Relative(repoRoot, counterpart)}. "
                        + "Every test needs both a stub and a reference implementation."));
                }
            }
        }

        return findings;
    }

    private static string Relative(string repoRoot, string path)
        => Path.GetRelativePath(repoRoot, path).Replace('\\', '/');
}
```

Create `tools/Training.Audit/Program.cs` — minimal for now, extended by later tasks:

```csharp
using Training.Audit;

var repoRoot = Directory.GetCurrentDirectory();
var command = args.Length > 0 ? args[0] : "all";

IReadOnlyList<AuditFinding> findings = command switch
{
    "pairs" => PairChecker.Run(repoRoot),
    "all" => PairChecker.Run(repoRoot),
    _ => throw new ArgumentException($"Unknown command '{command}'."),
};

foreach (var finding in findings)
{
    Console.Error.WriteLine($"[{finding.Check}] {finding.Path}: {finding.Message}");
}

Console.WriteLine(findings.Count == 0
    ? "audit: clean"
    : $"audit: {findings.Count} finding(s)");

return findings.Count == 0 ? 0 : 1;
```

- [ ] **Step 4: Run the tests to verify they pass**

Run: `dotnet test tools/Training.Audit.Tests`
Expected: **PASS**, 4 tests passed.

- [ ] **Step 5: Verify it runs against the real repository**

Run: `dotnet run --project tools/Training.Audit -- pairs`
Expected: `audit: clean`, exit code 0 — the probe module has all three files.

- [ ] **Step 6: Commit**

```bash
git add tools/Training.Audit tools/Training.Audit.Tests Directory.Packages.props
git commit -m "Add the audit tool and the file-pair check

Proves the half the solvability job cannot see: that the learner's stub exists.
A PR adding a test and a solution but no stub would otherwise go green."
```

---

## Task 4: API surface parity between Exercises and Solutions

**Files:**
- Create: `tools/Training.Audit/ApiSurfaceChecker.cs`
- Create: `tools/Training.Audit.Tests/ApiSurfaceCheckerTests.cs`
- Modify: `tools/Training.Audit/Training.Audit.csproj`, `tools/Training.Audit/Program.cs`

**Interfaces:**
- Consumes: `AuditFinding`, `RepoLayout.ModuleDirectories` from Task 3.
- Produces: `public static class ApiSurfaceChecker` with `IReadOnlyList<AuditFinding> Run(string repoRoot)` and `IReadOnlySet<string> Surface(string projectDirectory)`.

Why source-level rather than reflection: comparing compiled assemblies means loading them, resolving their dependencies, and reading nullability out of attributes. Comparing Roslyn syntax trees needs no build at all, sees `string?` directly as written, and catches parameter renames — which are invisible to a filename check and fatal to a learner whose stub no longer matches the test.

- [ ] **Step 1: Write the failing test**

Create `tools/Training.Audit.Tests/ApiSurfaceCheckerTests.cs`:

```csharp
using Shouldly;
using Training.Audit;

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

    public void Dispose() => Directory.Delete(_root, recursive: true);
}
```

- [ ] **Step 2: Run the test to verify it fails**

Run: `dotnet test tools/Training.Audit.Tests --filter-class Training.Audit.Tests.ApiSurfaceCheckerTests`
Expected: **FAIL** to compile — `ApiSurfaceChecker` does not exist.

- [ ] **Step 3: Write the implementation**

Add the Roslyn package to `tools/Training.Audit/Training.Audit.csproj`:

```xml
  <ItemGroup>
    <PackageReference Include="Microsoft.CodeAnalysis.CSharp" />
  </ItemGroup>
```

Create `tools/Training.Audit/ApiSurfaceChecker.cs`:

```csharp
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Training.Audit;

/// <summary>
/// src/Exercises and src/Solutions must expose the identical public API, because
/// the same test code compiles against whichever one MSBuild supplied.
///
/// A file-pair check cannot see a renamed parameter, an added overload, or a
/// nullability annotation that drifted — all of which hand the learner a test
/// that will not compile against their stub. This compares the two projects'
/// public declarations as written, with no build required.
/// </summary>
public static class ApiSurfaceChecker
{
    public const string Name = "api";

    public static IReadOnlyList<AuditFinding> Run(string repoRoot)
    {
        var findings = new List<AuditFinding>();

        foreach (var module in RepoLayout.ModuleDirectories(repoRoot))
        {
            var exercises = Path.Combine(module, "src", "Exercises");
            var solutions = Path.Combine(module, "src", "Solutions");

            if (!Directory.Exists(exercises) || !Directory.Exists(solutions))
            {
                continue;
            }

            var stubSurface = Surface(exercises);
            var solutionSurface = Surface(solutions);
            var modulePath = Path.GetRelativePath(repoRoot, module).Replace('\\', '/');

            foreach (var missing in solutionSurface.Except(stubSurface).Order(StringComparer.Ordinal))
            {
                findings.Add(new AuditFinding(
                    Name, modulePath,
                    $"src/Solutions declares `{missing}` but src/Exercises does not. "
                    + "The learner's stub must expose it too."));
            }

            foreach (var missing in stubSurface.Except(solutionSurface).Order(StringComparer.Ordinal))
            {
                findings.Add(new AuditFinding(
                    Name, modulePath,
                    $"src/Exercises declares `{missing}` but src/Solutions does not. "
                    + "The reference implementation must expose it too."));
            }
        }

        return findings;
    }

    /// <summary>Every publicly visible declaration in a project, as a normalised string.</summary>
    public static IReadOnlySet<string> Surface(string projectDirectory)
    {
        var surface = new HashSet<string>(StringComparer.Ordinal);

        foreach (var file in Directory.EnumerateFiles(projectDirectory, "*.cs", SearchOption.AllDirectories))
        {
            var tree = CSharpSyntaxTree.ParseText(File.ReadAllText(file));
            var root = tree.GetRoot();

            foreach (var type in root.DescendantNodes().OfType<BaseTypeDeclarationSyntax>())
            {
                if (!IsPubliclyVisible(type.Modifiers))
                {
                    continue;
                }

                var typeName = QualifiedName(type);
                surface.Add($"type {typeName}");

                if (type is not TypeDeclarationSyntax declaration)
                {
                    continue;
                }

                foreach (var member in declaration.Members)
                {
                    if (!IsPubliclyVisible(GetModifiers(member)))
                    {
                        continue;
                    }

                    surface.Add($"{typeName}.{Normalise(member)}");
                }
            }
        }

        return surface;
    }

    private static SyntaxTokenList GetModifiers(MemberDeclarationSyntax member) => member.Modifiers;

    private static bool IsPubliclyVisible(SyntaxTokenList modifiers)
        => modifiers.Any(m => m.IsKind(SyntaxKind.PublicKeyword) || m.IsKind(SyntaxKind.ProtectedKeyword));

    private static string QualifiedName(BaseTypeDeclarationSyntax type)
    {
        var namespaceName = type.Ancestors()
            .OfType<BaseNamespaceDeclarationSyntax>()
            .Select(n => n.Name.ToString())
            .FirstOrDefault() ?? string.Empty;

        var name = type.Identifier.Text;
        if (type is TypeDeclarationSyntax { TypeParameterList: { } parameters })
        {
            name += parameters.ToString();
        }

        return namespaceName.Length == 0 ? name : $"{namespaceName}.{name}";
    }

    /// <summary>
    /// A member declaration with its body, initialiser and trivia removed, so
    /// only the signature survives. `public int Add(int a) => a + 1;` and
    /// `public int Add(int a) => throw new NotImplementedException();` normalise
    /// to the same string; renaming `a` to `b` does not.
    /// </summary>
    private static string Normalise(MemberDeclarationSyntax member)
    {
        var signature = member switch
        {
            MethodDeclarationSyntax m => $"{m.ReturnType} {m.Identifier}{m.TypeParameterList}{m.ParameterList}",
            ConstructorDeclarationSyntax c => $"ctor {c.ParameterList}",
            PropertyDeclarationSyntax p => $"{p.Type} {p.Identifier} {{ {Accessors(p.AccessorList)} }}",
            IndexerDeclarationSyntax i => $"{i.Type} this{i.ParameterList}",
            EventDeclarationSyntax e => $"event {e.Type} {e.Identifier}",
            FieldDeclarationSyntax f => $"{f.Declaration.Type} {string.Join(",", f.Declaration.Variables.Select(v => v.Identifier.Text))}",
            OperatorDeclarationSyntax o => $"operator {o.OperatorToken} {o.ReturnType}{o.ParameterList}",
            ConversionOperatorDeclarationSyntax v => $"conversion {v.Type}{v.ParameterList}",
            _ => member.ToString(),
        };

        return string.Join(' ', signature.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
    }

    private static string Accessors(AccessorListSyntax? accessors)
        => accessors is null
            ? string.Empty
            : string.Join(" ", accessors.Accessors.Select(a => a.Keyword.Text + ";"));
}
```

Extend `tools/Training.Audit/Program.cs` — replace the `findings` assignment:

```csharp
IReadOnlyList<AuditFinding> findings = command switch
{
    "pairs" => PairChecker.Run(repoRoot),
    "api" => ApiSurfaceChecker.Run(repoRoot),
    "all" => [.. PairChecker.Run(repoRoot), .. ApiSurfaceChecker.Run(repoRoot)],
    _ => throw new ArgumentException($"Unknown command '{command}'."),
};
```

- [ ] **Step 4: Run the tests to verify they pass**

Run: `dotnet test tools/Training.Audit.Tests`
Expected: **PASS**, 9 tests passed (4 from Task 3, 5 new).

- [ ] **Step 5: Verify against the real repository**

Run: `dotnet run --project tools/Training.Audit -- api`
Expected: `audit: clean` — the probe module's two `Probe.cs` files declare the same signature.

- [ ] **Step 6: Prove it catches real drift**

Temporarily change `modules/00-probe/src/Solutions/Core/Probe.cs` to `public static int Answer(int seed) => 42;`, then run: `dotnet run --project tools/Training.Audit -- api`

Expected: **2 findings** — one for the signature present only in Solutions, one for the signature present only in Exercises. Revert the change and confirm `audit: clean` returns.

- [ ] **Step 7: Commit**

```bash
git add tools/Training.Audit tools/Training.Audit.Tests
git commit -m "Add public API parity between Exercises and Solutions

Compares Roslyn syntax trees rather than compiled assemblies: no build needed,
nullability is read as written, and parameter renames are caught. A filename
pair check sees none of those, and each one hands the learner a test that will
not compile against their stub."
```

---

## Task 5: Guide anatomy and the prose word count

**Files:**
- Create: `tools/Training.Audit/GuideText.cs`, `tools/Training.Audit/GuideAnatomyChecker.cs`
- Create: `tools/Training.Audit.Tests/GuideTextTests.cs`, `tools/Training.Audit.Tests/GuideAnatomyCheckerTests.cs`
- Modify: `tools/Training.Audit/Program.cs`

**Interfaces:**
- Consumes: `AuditFinding`, `RepoLayout.ModuleDirectories`.
- Produces:
  - `public static class GuideText` with `int CountProseWords(string markdown)` and `IReadOnlyList<string> SectionHeadings(string markdown)`
  - `public static class GuideAnatomyChecker` with `IReadOnlyList<AuditFinding> Run(string repoRoot)` and `static readonly string[] RequiredSections`

- [ ] **Step 1: Write the failing test**

Create `tools/Training.Audit.Tests/GuideTextTests.cs`:

```csharp
using Shouldly;
using Training.Audit;

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
```

Create `tools/Training.Audit.Tests/GuideAnatomyCheckerTests.cs`:

```csharp
using Shouldly;
using Training.Audit;

namespace Training.Audit.Tests;

public sealed class GuideAnatomyCheckerTests : IDisposable
{
    private readonly string _root = Directory.CreateTempSubdirectory("guide-tests").FullName;

    private void WriteGuide(string body)
    {
        var dir = Path.Combine(_root, "modules", "01-demo");
        Directory.CreateDirectory(dir);
        File.WriteAllText(Path.Combine(dir, "GUIDE.md"), body);
    }

    private static string GuideWith(int proseWords, IEnumerable<string>? sections = null)
    {
        var used = sections ?? GuideAnatomyChecker.RequiredSections;
        var body = string.Join("\n\n", used.Select(s => $"## {s}"));
        return "# Module 01\n\n" + body + "\n\n" + string.Join(' ', Enumerable.Repeat("word", proseWords));
    }

    [Fact]
    public void Accepts_a_guide_with_every_section_and_a_valid_word_count()
    {
        WriteGuide(GuideWith(3500));

        GuideAnatomyChecker.Run(_root).ShouldBeEmpty();
    }

    [Fact]
    public void Rejects_a_guide_that_is_too_short()
    {
        WriteGuide(GuideWith(1500));

        var findings = GuideAnatomyChecker.Run(_root);

        findings.ShouldNotBeEmpty();
        findings[0].Message.ShouldContain("1500");
        findings[0].Message.ShouldContain("3000");
    }

    [Fact]
    public void Rejects_a_guide_that_is_too_long()
    {
        WriteGuide(GuideWith(5200));

        GuideAnatomyChecker.Run(_root).ShouldNotBeEmpty();
    }

    [Fact]
    public void Rejects_a_missing_section()
    {
        WriteGuide(GuideWith(3500, GuideAnatomyChecker.RequiredSections.Where(s => s != "Exercises")));

        var findings = GuideAnatomyChecker.Run(_root);

        findings.ShouldNotBeEmpty();
        findings.ShouldContain(f => f.Message.Contains("Exercises"));
    }

    [Fact]
    public void Rejects_sections_that_are_out_of_order()
    {
        var reordered = GuideAnatomyChecker.RequiredSections.Reverse();
        WriteGuide(GuideWith(3500, reordered));

        GuideAnatomyChecker.Run(_root).ShouldNotBeEmpty();
    }

    public void Dispose() => Directory.Delete(_root, recursive: true);
}
```

- [ ] **Step 2: Run the tests to verify they fail**

Run: `dotnet test tools/Training.Audit.Tests`
Expected: **FAIL** to compile — `GuideText` and `GuideAnatomyChecker` do not exist.

- [ ] **Step 3: Write the implementation**

Create `tools/Training.Audit/GuideText.cs`:

```csharp
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

    /// <summary>The level-two headings, in document order.</summary>
    public static IReadOnlyList<string> SectionHeadings(string markdown)
    {
        var headings = new List<string>();
        var inFence = false;

        foreach (var raw in markdown.Split('\n'))
        {
            var trimmed = raw.TrimEnd('\r').TrimStart();

            if (trimmed.StartsWith("```", StringComparison.Ordinal))
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
```

Create `tools/Training.Audit/GuideAnatomyChecker.cs`:

```csharp
namespace Training.Audit;

/// <summary>
/// Every guide has the same eight sections in the same order, and 3,000–5,000
/// prose words. Under 3,000 means the module is incomplete: the author stopped
/// before the material did.
/// </summary>
public static class GuideAnatomyChecker
{
    public const string Name = "guides";
    public const int MinimumWords = 3000;
    public const int MaximumWords = 5000;

    public static readonly string[] RequiredSections =
    [
        "Before you start",
        "Objectives",
        "Sections",
        "Real-world case",
        "Exercises",
        "Summary",
        "Review questions",
        "Resources",
    ];

    public static IReadOnlyList<AuditFinding> Run(string repoRoot)
    {
        var findings = new List<AuditFinding>();

        foreach (var module in RepoLayout.ModuleDirectories(repoRoot))
        {
            var guidePath = Path.Combine(module, "GUIDE.md");
            var relative = Path.GetRelativePath(repoRoot, guidePath).Replace('\\', '/');

            if (!File.Exists(guidePath))
            {
                findings.Add(new AuditFinding(Name, relative, "is missing."));
                continue;
            }

            var markdown = File.ReadAllText(guidePath);
            var headings = GuideText.SectionHeadings(markdown);
            var position = 0;

            foreach (var required in RequiredSections)
            {
                var index = headings.ToList().IndexOf(required);

                if (index < 0)
                {
                    findings.Add(new AuditFinding(
                        Name, relative,
                        $"is missing the required section '{required}'."));
                }
                else if (index < position)
                {
                    findings.Add(new AuditFinding(
                        Name, relative,
                        $"has '{required}' out of order. The anatomy is fixed: "
                        + string.Join(" → ", RequiredSections)));
                }
                else
                {
                    position = index;
                }
            }

            var words = GuideText.CountProseWords(markdown);
            if (words < MinimumWords || words > MaximumWords)
            {
                findings.Add(new AuditFinding(
                    Name, relative,
                    $"has {words} prose words, outside the required {MinimumWords}–{MaximumWords}. "
                    + (words < MinimumWords
                        ? "Go back to the source and find what is missing."
                        : "Split the material or cut the padding.")));
            }
        }

        return findings;
    }
}
```

Extend `tools/Training.Audit/Program.cs`:

```csharp
IReadOnlyList<AuditFinding> findings = command switch
{
    "pairs" => PairChecker.Run(repoRoot),
    "api" => ApiSurfaceChecker.Run(repoRoot),
    "guides" => GuideAnatomyChecker.Run(repoRoot),
    "all" =>
    [
        .. PairChecker.Run(repoRoot),
        .. ApiSurfaceChecker.Run(repoRoot),
        .. GuideAnatomyChecker.Run(repoRoot),
    ],
    _ => throw new ArgumentException($"Unknown command '{command}'."),
};
```

- [ ] **Step 4: Run the tests to verify they pass**

Run: `dotnet test tools/Training.Audit.Tests`
Expected: **PASS**, 19 tests passed.

- [ ] **Step 5: Commit**

```bash
git add tools/Training.Audit tools/Training.Audit.Tests
git commit -m "Enforce guide anatomy and the prose word count

Eight sections in fixed order, 3000-5000 words counted over prose only so the
gate cannot be satisfied with code listings and tables."
```

---

## Task 6: TRX parsing and the stub-leak check

**Files:**
- Create: `tools/Training.Audit/TrxReport.cs`, `tools/Training.Audit/StubLeakChecker.cs`
- Create: `tools/Training.Audit.Tests/TrxReportTests.cs`, `tools/Training.Audit.Tests/StubLeakCheckerTests.cs`

**Interfaces:**
- Consumes: `AuditFinding`.
- Produces:
  - `public sealed record TrxTest(string ClassName, string MethodName, string Outcome, string CodeBase)` with `bool Failed => Outcome == "Failed"`
  - `public sealed class TrxReport` with `static TrxReport Load(string path)` — accepting **either** a single `.trx` file **or** a directory of them, because `dotnet test` runs one project at a time and each writes its own report — and `IReadOnlyList<TrxTest> Tests { get; }`
  - `public static class StubLeakChecker` with `IReadOnlyList<AuditFinding> Run(TrxReport report)`

Why per-class rather than per-run: the sibling Python repo asserts the whole suite fails without solutions. That catches a total leak but not a partial one — if one module's stub ships with the answer, that module goes green, every other module stays red, the aggregate run still fails, and CI reports success at catching nothing. Requiring **every exercise test class to contain at least one failure** closes that hole in a single run.

- [ ] **Step 1: Write the failing test**

Create `tools/Training.Audit.Tests/TrxReportTests.cs`:

```csharp
using Shouldly;
using Training.Audit;

namespace Training.Audit.Tests;

public sealed class TrxReportTests : IDisposable
{
    private readonly string _file = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.trx");

    private const string Sample = """
        <?xml version="1.0" encoding="UTF-8"?>
        <TestRun xmlns="http://microsoft.com/schemas/VisualStudio/TeamTest/2010">
          <Results>
            <UnitTestResult testId="a" testName="Training.Module01.Tests.Core.MoneyTests.Adds" outcome="Failed" />
            <UnitTestResult testId="b" testName="Training.Module01.Tests.Core.MoneyTests.Rejects" outcome="Passed" />
          </Results>
          <TestDefinitions>
            <UnitTest id="a" name="Adds">
              <TestMethod codeBase="/repo/modules/01-demo/tests/UnitTests/bin/Module01.UnitTests.dll"
                          className="Training.Module01.Tests.Core.MoneyTests" name="Adds" />
            </UnitTest>
            <UnitTest id="b" name="Rejects">
              <TestMethod codeBase="/repo/modules/01-demo/tests/UnitTests/bin/Module01.UnitTests.dll"
                          className="Training.Module01.Tests.Core.MoneyTests" name="Rejects" />
            </UnitTest>
          </TestDefinitions>
        </TestRun>
        """;

    [Fact]
    public void Reads_every_result_with_its_class_and_outcome()
    {
        File.WriteAllText(_file, Sample);

        var report = TrxReport.Load(_file);

        report.Tests.Count.ShouldBe(2);
        report.Tests.ShouldAllBe(t => t.ClassName == "Training.Module01.Tests.Core.MoneyTests");
        report.Tests.Count(t => t.Failed).ShouldBe(1);
    }

    [Fact]
    public void Reads_the_code_base_so_results_can_be_traced_to_a_module()
    {
        File.WriteAllText(_file, Sample);

        TrxReport.Load(_file).Tests[0].CodeBase.ShouldContain("modules/01-demo");
    }

    [Fact]
    public void Merges_every_report_in_a_directory()
    {
        // dotnet test runs one project at a time, so a full run leaves one
        // .trx per module rather than one for the run.
        var directory = Directory.CreateTempSubdirectory("trx-dir").FullName;
        File.WriteAllText(Path.Combine(directory, "01.trx"), Sample);
        File.WriteAllText(Path.Combine(directory, "02.trx"), Sample);

        try
        {
            TrxReport.Load(directory).Tests.Count.ShouldBe(4);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    public void Dispose()
    {
        if (File.Exists(_file))
        {
            File.Delete(_file);
        }
    }
}
```

Create `tools/Training.Audit.Tests/StubLeakCheckerTests.cs`:

```csharp
using Shouldly;
using Training.Audit;

namespace Training.Audit.Tests;

public sealed class StubLeakCheckerTests
{
    private static TrxTest Test(string className, string method, string outcome)
        => new(className, method, outcome, "/repo/modules/01-demo/tests/UnitTests/bin/x.dll");

    [Fact]
    public void Accepts_a_class_with_at_least_one_failure()
    {
        var report = TrxReport.FromTests(
        [
            Test("Training.Module01.Tests.Core.MoneyTests", "Adds", "Failed"),
            Test("Training.Module01.Tests.Core.MoneyTests", "Rejects", "Failed"),
        ]);

        StubLeakChecker.Run(report).ShouldBeEmpty();
    }

    [Fact]
    public void Reports_a_class_where_every_test_passed_against_the_stubs()
    {
        var report = TrxReport.FromTests(
        [
            Test("Training.Module14.Tests.Core.MediatorTests", "Dispatches", "Passed"),
            Test("Training.Module14.Tests.Core.MediatorTests", "Orders", "Passed"),
        ]);

        var findings = StubLeakChecker.Run(report);

        findings.Count.ShouldBe(1);
        findings[0].Message.ShouldContain("already contains the answer");
    }

    [Fact]
    public void Catches_a_partial_leak_that_an_aggregate_check_would_miss()
    {
        // Module 14 leaked; module 01 is still red. A whole-run assertion would
        // see "the suite failed" and report success at catching nothing.
        var report = TrxReport.FromTests(
        [
            Test("Training.Module01.Tests.Core.MoneyTests", "Adds", "Failed"),
            Test("Training.Module14.Tests.Core.MediatorTests", "Dispatches", "Passed"),
        ]);

        StubLeakChecker.Run(report).Count.ShouldBe(1);
    }

    [Fact]
    public void Ignores_tests_outside_the_modules_folder()
    {
        var report = TrxReport.FromTests(
        [
            new TrxTest("Training.Audit.Tests.PairCheckerTests", "Works", "Passed", "/repo/tools/x.dll"),
        ]);

        StubLeakChecker.Run(report).ShouldBeEmpty();
    }
}
```

- [ ] **Step 2: Run the tests to verify they fail**

Run: `dotnet test tools/Training.Audit.Tests`
Expected: **FAIL** to compile — `TrxReport` and `StubLeakChecker` do not exist.

- [ ] **Step 3: Write the implementation**

Create `tools/Training.Audit/TrxReport.cs`:

```csharp
using System.Xml.Linq;

namespace Training.Audit;

public sealed record TrxTest(string ClassName, string MethodName, string Outcome, string CodeBase)
{
    public bool Failed => string.Equals(Outcome, "Failed", StringComparison.OrdinalIgnoreCase);
}

/// <summary>
/// A parsed TRX file, produced by Microsoft.Testing.Extensions.TrxReport.
/// Element names are matched on local name so the schema namespace cannot
/// break parsing when the extension version moves.
/// </summary>
public sealed class TrxReport
{
    private TrxReport(IReadOnlyList<TrxTest> tests) => Tests = tests;

    public IReadOnlyList<TrxTest> Tests { get; }

    public static TrxReport FromTests(IReadOnlyList<TrxTest> tests) => new(tests);

    /// <summary>
    /// Loads one .trx file, or merges every .trx under a directory. `dotnet test`
    /// runs a single project at a time, so a full-repo run produces one report
    /// per module rather than one for the run.
    /// </summary>
    public static TrxReport Load(string path)
    {
        if (Directory.Exists(path))
        {
            var merged = Directory
                .EnumerateFiles(path, "*.trx", SearchOption.AllDirectories)
                .OrderBy(f => f, StringComparer.Ordinal)
                .SelectMany(file => LoadFile(file).Tests)
                .ToList();

            return new TrxReport(merged);
        }

        return LoadFile(path);
    }

    private static TrxReport LoadFile(string path)
    {
        var document = XDocument.Load(path);

        var definitions = document.Descendants()
            .Where(e => e.Name.LocalName == "UnitTest")
            .Select(unitTest => new
            {
                Id = unitTest.Attribute("id")?.Value ?? string.Empty,
                Method = unitTest.Elements().FirstOrDefault(e => e.Name.LocalName == "TestMethod"),
            })
            .Where(x => x.Id.Length > 0 && x.Method is not null)
            .ToDictionary(
                x => x.Id,
                x => (
                    ClassName: x.Method!.Attribute("className")?.Value ?? string.Empty,
                    CodeBase: (x.Method.Attribute("codeBase")?.Value ?? string.Empty).Replace('\\', '/')),
                StringComparer.Ordinal);

        var tests = new List<TrxTest>();

        foreach (var result in document.Descendants().Where(e => e.Name.LocalName == "UnitTestResult"))
        {
            var id = result.Attribute("testId")?.Value ?? string.Empty;
            var outcome = result.Attribute("outcome")?.Value ?? "Unknown";
            var testName = result.Attribute("testName")?.Value ?? string.Empty;

            var className = definitions.TryGetValue(id, out var definition)
                ? definition.ClassName
                : ClassNameFrom(testName);

            var codeBase = definition.CodeBase ?? string.Empty;
            var methodName = testName.Length > className.Length && className.Length > 0
                ? testName[(className.Length + 1)..]
                : testName;

            tests.Add(new TrxTest(className, methodName, outcome, codeBase));
        }

        return new TrxReport(tests);
    }

    private static string ClassNameFrom(string testName)
    {
        var lastDot = testName.LastIndexOf('.');
        return lastDot < 0 ? testName : testName[..lastDot];
    }
}
```

Create `tools/Training.Audit/StubLeakChecker.cs`:

```csharp
namespace Training.Audit;

/// <summary>
/// Run without -p:UseSolutions=true, every exercise test class must contain at
/// least one failure. A class that is entirely green means its stub in
/// src/Exercises already carries the answer.
/// </summary>
public static class StubLeakChecker
{
    public const string Name = "stub-leak";

    public static IReadOnlyList<AuditFinding> Run(TrxReport report)
    {
        var findings = new List<AuditFinding>();

        var exerciseTests = report.Tests
            .Where(t => t.CodeBase.Contains("/modules/", StringComparison.Ordinal))
            .GroupBy(t => t.ClassName, StringComparer.Ordinal)
            .OrderBy(g => g.Key, StringComparer.Ordinal);

        foreach (var testClass in exerciseTests)
        {
            if (testClass.Any(t => t.Failed))
            {
                continue;
            }

            findings.Add(new AuditFinding(
                Name,
                testClass.Key,
                $"passed entirely without -p:UseSolutions=true, across {testClass.Count()} test(s). "
                + "That means the stub in src/Exercises already contains the answer."));
        }

        return findings;
    }
}
```

- [ ] **Step 4: Run the tests to verify they pass**

Run: `dotnet test tools/Training.Audit.Tests`
Expected: **PASS**, 26 tests passed.

- [ ] **Step 5: Commit**

```bash
git add tools/Training.Audit tools/Training.Audit.Tests
git commit -m "Detect leaked answers per test class, not per run

A whole-suite assertion cannot see a partial leak: one green module inside a
red run still fails the run, so the check reports success at catching nothing.
Requiring every exercise test class to have at least one failure closes it."
```

---

## Task 7: Status reporting and the finished CLI

**Files:**
- Create: `tools/Training.Audit/StatusReporter.cs`, `tools/Training.Audit.Tests/StatusReporterTests.cs`
- Modify: `tools/Training.Audit/Program.cs`

**Interfaces:**
- Consumes: `TrxReport`, `TrxTest`, all four checkers.
- Produces: `public static class StatusReporter` with `string Render(TrxReport report)`; a `Program` accepting `all`, `pairs`, `api`, `guides`, `stub-leak --trx <path>`, `status --trx <path>`.

`status` reuses the same TRX the stub-leak check parses, so a learner's progress view and a CI gate share one code path rather than drifting apart.

- [ ] **Step 1: Write the failing test**

Create `tools/Training.Audit.Tests/StatusReporterTests.cs`:

```csharp
using Shouldly;
using Training.Audit;

namespace Training.Audit.Tests;

public sealed class StatusReporterTests
{
    private static TrxTest InModule(string module, string method, string outcome)
        => new($"Training.Tests.{method}Tests", method, outcome,
               $"/repo/modules/{module}/tests/UnitTests/bin/Debug/net10.0/x.dll");

    [Fact]
    public void Groups_results_by_module_in_numeric_order()
    {
        var report = TrxReport.FromTests(
        [
            InModule("03-async-await-and-the-thread-pool", "Deadlock", "Failed"),
            InModule("01-type-system-and-memory", "Money", "Passed"),
        ]);

        var output = StatusReporter.Render(report);
        var moduleOne = output.IndexOf("01-type-system-and-memory", StringComparison.Ordinal);
        var moduleThree = output.IndexOf("03-async-await-and-the-thread-pool", StringComparison.Ordinal);

        moduleOne.ShouldBeLessThan(moduleThree);
    }

    [Fact]
    public void Shows_solved_over_total_for_each_module()
    {
        var report = TrxReport.FromTests(
        [
            InModule("01-type-system-and-memory", "Money", "Passed"),
            InModule("01-type-system-and-memory", "Basket", "Passed"),
            InModule("01-type-system-and-memory", "Boxing", "Failed"),
        ]);

        StatusReporter.Render(report).ShouldContain("2/3");
    }

    [Fact]
    public void Marks_a_fully_solved_module_as_done()
    {
        var report = TrxReport.FromTests([InModule("01-type-system-and-memory", "Money", "Passed")]);

        StatusReporter.Render(report).ShouldContain("done");
    }

    [Fact]
    public void Reports_an_empty_run_without_throwing()
    {
        StatusReporter.Render(TrxReport.FromTests([])).ShouldContain("No module tests");
    }
}
```

- [ ] **Step 2: Run the test to verify it fails**

Run: `dotnet test tools/Training.Audit.Tests --filter-class Training.Audit.Tests.StatusReporterTests`
Expected: **FAIL** to compile — `StatusReporter` does not exist.

- [ ] **Step 3: Write the implementation**

Create `tools/Training.Audit/StatusReporter.cs`:

```csharp
using System.Text;
using System.Text.RegularExpressions;

namespace Training.Audit;

/// <summary>
/// A per-module progress table, derived from a normal (non-solutions) test run.
/// Failing means unsolved, which is the correct starting state.
///
/// Thirty modules is long enough that people lose their place, and a visible
/// map of what is done is the difference between a path someone returns to and
/// a repo someone abandons in tier 3.
/// </summary>
public static partial class StatusReporter
{
    public static string Render(TrxReport report)
    {
        var byModule = report.Tests
            .Select(t => (Module: ModuleFrom(t.CodeBase), Test: t))
            .Where(x => x.Module is not null)
            .GroupBy(x => x.Module!, StringComparer.Ordinal)
            .OrderBy(g => g.Key, StringComparer.Ordinal)
            .ToList();

        if (byModule.Count == 0)
        {
            return "No module tests in this run.";
        }

        var builder = new StringBuilder();
        builder.AppendLine("module                                    solved   state");
        builder.AppendLine("--------------------------------------------------------");

        foreach (var module in byModule)
        {
            var total = module.Count();
            var solved = module.Count(x => !x.Test.Failed);
            var state = solved == total ? "done" : solved == 0 ? "not started" : "in progress";

            builder.AppendLine($"{module.Key,-40}  {solved}/{total,-5}  {state}");
        }

        var allTests = byModule.Sum(m => m.Count());
        var allSolved = byModule.Sum(m => m.Count(x => !x.Test.Failed));
        builder.AppendLine("--------------------------------------------------------");
        builder.AppendLine($"{"total",-40}  {allSolved}/{allTests}");

        return builder.ToString();
    }

    private static string? ModuleFrom(string codeBase)
    {
        var match = ModulePath().Match(codeBase);
        return match.Success ? match.Groups[1].Value : null;
    }

    [GeneratedRegex("/modules/([^/]+)/")]
    private static partial Regex ModulePath();
}
```

Replace `tools/Training.Audit/Program.cs` entirely:

```csharp
using Training.Audit;

var repoRoot = Directory.GetCurrentDirectory();
var command = args.Length > 0 ? args[0] : "all";
var trxPath = ArgumentValue(args, "--trx");

if (command == "status")
{
    if (trxPath is null)
    {
        Console.Error.WriteLine("status requires --trx <path>");
        return 2;
    }

    Console.WriteLine(StatusReporter.Render(TrxReport.Load(trxPath)));
    return 0;
}

IReadOnlyList<AuditFinding> findings;

switch (command)
{
    case "pairs":
        findings = PairChecker.Run(repoRoot);
        break;
    case "api":
        findings = ApiSurfaceChecker.Run(repoRoot);
        break;
    case "guides":
        findings = GuideAnatomyChecker.Run(repoRoot);
        break;
    case "stub-leak":
        if (trxPath is null)
        {
            Console.Error.WriteLine("stub-leak requires --trx <path>");
            return 2;
        }

        findings = StubLeakChecker.Run(TrxReport.Load(trxPath));
        break;
    case "all":
        findings =
        [
            .. PairChecker.Run(repoRoot),
            .. ApiSurfaceChecker.Run(repoRoot),
            .. GuideAnatomyChecker.Run(repoRoot),
        ];
        break;
    default:
        Console.Error.WriteLine(
            "usage: audit [all|pairs|api|guides] | audit stub-leak --trx <path> | audit status --trx <path>");
        return 2;
}

foreach (var finding in findings)
{
    Console.Error.WriteLine($"[{finding.Check}] {finding.Path}: {finding.Message}");
}

Console.WriteLine(findings.Count == 0 ? "audit: clean" : $"audit: {findings.Count} finding(s)");
return findings.Count == 0 ? 0 : 1;

static string? ArgumentValue(string[] arguments, string name)
{
    var index = Array.IndexOf(arguments, name);
    return index >= 0 && index + 1 < arguments.Length ? arguments[index + 1] : null;
}
```

- [ ] **Step 4: Run the tests to verify they pass**

Run: `dotnet test tools/Training.Audit.Tests`
Expected: **PASS**, 30 tests passed.

- [ ] **Step 5: Verify the CLI end to end**

Run: `dotnet run --project tools/Training.Audit -- all`
Expected: findings for `modules/00-probe` missing `GUIDE.md` — the probe has no guide, which is correct. Note the exit code is 1.

Run: `dotnet run --project tools/Training.Audit -- pairs`
Expected: `audit: clean`, exit code 0.

- [ ] **Step 6: Commit**

```bash
git add tools/Training.Audit tools/Training.Audit.Tests
git commit -m "Add the status report and finish the audit CLI

Progress view and the stub-leak gate parse the same TRX, so the learner's map
and CI cannot drift apart."
```

---

## Task 8: The module scaffolder

**Files:**
- Create: `tools/Training.Scaffold/Training.Scaffold.csproj`, `tools/Training.Scaffold/Program.cs`, `tools/Training.Scaffold/ModuleTemplate.cs`
- Create: `tools/Training.Scaffold.Tests/Training.Scaffold.Tests.csproj`, `tools/Training.Scaffold.Tests/ModuleTemplateTests.cs`

**Interfaces:**
- Consumes: `GuideAnatomyChecker.RequiredSections` (project reference to `Training.Audit`), so the scaffolder cannot emit a guide skeleton the audit rejects.
- Produces: `public static class ModuleTemplate` with `void Create(string repoRoot, string slug, string title, int number)`.

- [ ] **Step 1: Write the failing test**

Create `tools/Training.Scaffold.Tests/ModuleTemplateTests.cs`:

```csharp
using Shouldly;
using Training.Audit;
using Training.Scaffold;

namespace Training.Scaffold.Tests;

public sealed class ModuleTemplateTests : IDisposable
{
    private readonly string _root = Directory.CreateTempSubdirectory("scaffold-tests").FullName;

    private string Path(params string[] parts)
        => System.IO.Path.Combine([_root, "modules", "07-the-middleware-pipeline", .. parts]);

    public ModuleTemplateTests()
        => ModuleTemplate.Create(_root, "07-the-middleware-pipeline", "The middleware pipeline", 7);

    [Fact]
    public void Creates_the_three_projects()
    {
        File.Exists(Path("src", "Exercises", "Exercises.csproj")).ShouldBeTrue();
        File.Exists(Path("src", "Solutions", "Solutions.csproj")).ShouldBeTrue();
        File.Exists(Path("tests", "UnitTests", "UnitTests.csproj")).ShouldBeTrue();
    }

    [Fact]
    public void Marks_the_test_project_so_the_swap_target_applies()
    {
        File.ReadAllText(Path("tests", "UnitTests", "UnitTests.csproj"))
            .ShouldContain("<IsTrainingTestProject>true</IsTrainingTestProject>");
    }

    [Fact]
    public void Gives_both_source_projects_the_same_root_namespace()
    {
        const string expected = "<RootNamespace>Training.Module07</RootNamespace>";

        File.ReadAllText(Path("src", "Exercises", "Exercises.csproj")).ShouldContain(expected);
        File.ReadAllText(Path("src", "Solutions", "Solutions.csproj")).ShouldContain(expected);
    }

    [Fact]
    public void Writes_the_scoped_analyser_relaxation_only_under_Exercises()
    {
        var relaxation = File.ReadAllText(Path("src", "Exercises", ".editorconfig"));

        relaxation.ShouldContain("IDE0060");
        relaxation.ShouldContain("CA1801");
        relaxation.ShouldContain("CA1822");
        relaxation.ShouldContain("CS1998");
        File.Exists(Path("src", "Solutions", ".editorconfig")).ShouldBeFalse();
    }

    [Fact]
    public void Writes_a_guide_skeleton_with_every_required_section_in_order()
    {
        var headings = GuideText.SectionHeadings(File.ReadAllText(Path("GUIDE.md")));

        headings.ShouldBe(GuideAnatomyChecker.RequiredSections);
    }

    [Fact]
    public void Creates_the_Core_and_Challenge_folders_in_both_source_projects()
    {
        Directory.Exists(Path("src", "Exercises", "Core")).ShouldBeTrue();
        Directory.Exists(Path("src", "Exercises", "Challenge")).ShouldBeTrue();
        Directory.Exists(Path("src", "Solutions", "Core")).ShouldBeTrue();
        Directory.Exists(Path("src", "Solutions", "Challenge")).ShouldBeTrue();
    }

    public void Dispose() => Directory.Delete(_root, recursive: true);
}
```

Create `tools/Training.Scaffold.Tests/Training.Scaffold.Tests.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <RootNamespace>Training.Scaffold.Tests</RootNamespace>
    <OutputType>Exe</OutputType>
    <TestingPlatformDotnetTestSupport>true</TestingPlatformDotnetTestSupport>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="xunit.v3" />
    <PackageReference Include="Shouldly" />
    <PackageReference Include="Microsoft.Testing.Extensions.TrxReport" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\Training.Scaffold\Training.Scaffold.csproj" />
    <ProjectReference Include="..\Training.Audit\Training.Audit.csproj" />
  </ItemGroup>

  <ItemGroup>
    <Using Include="Xunit" />
  </ItemGroup>
</Project>
```

- [ ] **Step 2: Run the test to verify it fails**

Run: `dotnet test tools/Training.Scaffold.Tests`
Expected: **FAIL** to compile — `Training.Scaffold` does not exist.

- [ ] **Step 3: Write the implementation**

Create `tools/Training.Scaffold/Training.Scaffold.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <RootNamespace>Training.Scaffold</RootNamespace>
    <OutputType>Exe</OutputType>
  </PropertyGroup>

  <ItemGroup>
    <ProjectReference Include="..\Training.Audit\Training.Audit.csproj" />
  </ItemGroup>
</Project>
```

Create `tools/Training.Scaffold/ModuleTemplate.cs`:

```csharp
using Training.Audit;

namespace Training.Scaffold;

/// <summary>
/// Generates one module's three projects plus a guide skeleton.
///
/// It takes the required section list from GuideAnatomyChecker rather than
/// repeating it, so a scaffolded module can never start life failing the audit.
/// </summary>
public static class ModuleTemplate
{
    public static void Create(string repoRoot, string slug, string title, int number)
    {
        var module = Path.Combine(repoRoot, "modules", slug);
        var rootNamespace = $"Training.Module{number:D2}";

        foreach (var folder in (string[])["Core", "Challenge"])
        {
            Directory.CreateDirectory(Path.Combine(module, "src", "Exercises", folder));
            Directory.CreateDirectory(Path.Combine(module, "src", "Solutions", folder));
            Directory.CreateDirectory(Path.Combine(module, "tests", "UnitTests", folder));
        }

        Directory.CreateDirectory(Path.Combine(module, "examples"));

        Write(Path.Combine(module, "src", "Exercises", "Exercises.csproj"),
            SourceProject(rootNamespace, "Exercises"));
        Write(Path.Combine(module, "src", "Solutions", "Solutions.csproj"),
            SourceProject(rootNamespace, "Solutions"));
        Write(Path.Combine(module, "src", "Exercises", ".editorconfig"), StubRelaxation);
        Write(Path.Combine(module, "tests", "UnitTests", "UnitTests.csproj"),
            TestProject(rootNamespace, number));
        Write(Path.Combine(module, "GUIDE.md"), Guide(title, number));
    }

    private static void Write(string path, string content)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, content);
    }

    private static string SourceProject(string rootNamespace, string assemblyName) =>
        $"""
        <Project Sdk="Microsoft.NET.Sdk">
          <PropertyGroup>
            <RootNamespace>{rootNamespace}</RootNamespace>
            <AssemblyName>{assemblyName}</AssemblyName>
          </PropertyGroup>
        </Project>

        """;

    private static string TestProject(string rootNamespace, int number) =>
        $"""
        <Project Sdk="Microsoft.NET.Sdk">
          <PropertyGroup>
            <RootNamespace>{rootNamespace}.Tests</RootNamespace>
            <AssemblyName>Module{number:D2}.UnitTests</AssemblyName>
            <OutputType>Exe</OutputType>
            <TestingPlatformDotnetTestSupport>true</TestingPlatformDotnetTestSupport>
            <IsTrainingTestProject>true</IsTrainingTestProject>
          </PropertyGroup>

          <ItemGroup>
            <PackageReference Include="xunit.v3" />
            <PackageReference Include="Shouldly" />
            <PackageReference Include="Microsoft.Testing.Extensions.TrxReport" />
          </ItemGroup>

          <ItemGroup>
            <Using Include="Xunit" />
          </ItemGroup>
        </Project>

        """;

    /// <summary>
    /// The only analyser relaxation in the repository, scoped to stubs.
    /// Every rule here fires solely because the body is a throw, and stops
    /// applying the moment the learner writes a real implementation.
    /// </summary>
    private const string StubRelaxation =
        """
        # Scoped relaxation for exercise stubs ONLY. Do not copy this elsewhere.
        #
        # Each rule below fires purely because the method body is
        # `throw new NotImplementedException()`. Once the learner implements the
        # method, the rule applies again on its own. Solutions, examples, tools
        # and system checkpoints are held to the full ruleset.

        [*.cs]
        dotnet_diagnostic.IDE0060.severity = none   # unused parameter: nothing consumes it yet
        dotnet_diagnostic.CA1801.severity = none    # unused parameter, from the analyzer package
        dotnet_diagnostic.CA1822.severity = none    # could be static: never touches `this` yet
        dotnet_diagnostic.CS1998.severity = none    # async without await: the body only throws

        """;

    private static string Guide(string title, int number)
    {
        var sections = string.Join(
            "\n\n",
            GuideAnatomyChecker.RequiredSections.Select(section => $"## {section}"));

        return $"# Module {number:D2} · {title}\n\n{sections}\n";
    }
}
```

Create `tools/Training.Scaffold/Program.cs`:

```csharp
using Training.Scaffold;

if (args is not ["new-module", var slug, var title])
{
    Console.Error.WriteLine("""
        usage: dotnet run --project tools/Training.Scaffold -- new-module <slug> "<title>"
        example: dotnet run --project tools/Training.Scaffold -- new-module 07-the-middleware-pipeline "The middleware pipeline"
        """);
    return 2;
}

if (!int.TryParse(slug.AsSpan(0, 2), out var number))
{
    Console.Error.WriteLine($"Slug must start with a two-digit module number: '{slug}'");
    return 2;
}

ModuleTemplate.Create(Directory.GetCurrentDirectory(), slug, title, number);
Console.WriteLine($"Created modules/{slug}");
return 0;
```

- [ ] **Step 4: Run the tests to verify they pass**

Run: `dotnet test tools/Training.Scaffold.Tests`
Expected: **PASS**, 6 tests passed.

- [ ] **Step 5: Commit**

```bash
git add tools/Training.Scaffold tools/Training.Scaffold.Tests
git commit -m "Add the module scaffolder

Takes the required guide sections from the audit tool rather than repeating
them, so a generated module cannot start life failing its own anatomy check."
```

---

## Task 9: The learner's entry point

**Files:**
- Create: `run.sh`, `run.ps1`

**Interfaces:**
- Consumes: `Training.Audit` subcommands `status --trx` and `stub-leak --trx`.
- Produces: `./run.sh test [NN]`, `./run.sh status`, `./run.sh reset NN`.

- [ ] **Step 1: Write `run.sh`**

```bash
#!/usr/bin/env bash
# The learner's entry point. Three verbs, nothing clever.
set -euo pipefail

REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
cd "$REPO_ROOT"

usage() {
  cat <<'USAGE'
usage:
  ./run.sh test [NN]   run a module's unit tests (all modules if NN omitted)
  ./run.sh status      show which modules are solved
  ./run.sh reset NN    restore a module's stubs so you can do it again

  NN is the module number, e.g. 03. Integration tests are excluded from
  `test` by design: they need Docker. Run them with
  `dotnet test modules/NN-*/tests/IntegrationTests`.
USAGE
}

module_path() {
  local number="$1"
  local matches=(modules/"${number}"-*)
  if [[ ! -d "${matches[0]}" ]]; then
    echo "No module numbered ${number}." >&2
    exit 1
  fi
  echo "${matches[0]}"
}

case "${1:-}" in
  test)
    if [[ -n "${2:-}" ]]; then
      dotnet test "$(module_path "$2")/tests/UnitTests"
    else
      # dotnet test takes one project at a time, so iterate rather than glob.
      for project in modules/*/tests/UnitTests; do
        dotnet test "$project"
      done
    fi
    ;;

  status)
    mkdir -p artifacts
    rm -f artifacts/*.trx
    for project in modules/*/tests/UnitTests; do
      name="$(basename "$(dirname "$(dirname "$project")")")"
      # A non-zero exit is expected: unsolved exercises are failing tests.
      dotnet test "$project" \
        --report-trx --report-trx-filename "$name.trx" --results-directory artifacts || true
    done
    dotnet run --project tools/Training.Audit -- status --trx artifacts
    ;;

  reset)
    [[ -n "${2:-}" ]] || { usage; exit 2; }
    target="$(module_path "$2")/src/Exercises"

    # Refuse only if there is uncommitted work OUTSIDE the module being reset.
    # Work inside it is exactly what the learner is choosing to discard.
    outside="$(git status --porcelain -- . ":(exclude)${target}" | wc -l | tr -d ' ')"
    if [[ "$outside" != "0" ]]; then
      echo "You have uncommitted changes outside ${target}." >&2
      echo "Commit or stash them first — reset should never touch them." >&2
      exit 1
    fi

    echo "About to discard your work in ${target}:"
    git status --porcelain -- "$target"
    read -r -p "Type the module number again to confirm: " confirm
    [[ "$confirm" == "$2" ]] || { echo "Cancelled."; exit 1; }

    git checkout HEAD -- "$target"
    echo "Reset ${target}. Run ./run.sh test $2 to start again."
    ;;

  *)
    usage
    exit 2
    ;;
esac
```

- [ ] **Step 2: Verify `test` works**

Run: `chmod +x run.sh && ./run.sh test 00`
Expected: the probe module's test runs and **fails** with `NotImplementedException`. That is the correct state.

- [ ] **Step 3: Verify `status` works**

Run: `./run.sh status`
Expected: a table containing `00-probe` with `0/1` and `not started`.

- [ ] **Step 4: Verify `reset` refuses to eat unrelated work**

Run: `echo "scratch" > scratch.txt && ./run.sh reset 00`
Expected: **FAIL** with "You have uncommitted changes outside …". Then `rm scratch.txt`.

- [ ] **Step 5: Write `run.ps1` with the same three verbs**

```powershell
#!/usr/bin/env pwsh
# The learner's entry point on Windows. Mirrors run.sh exactly.
param(
    [Parameter(Position = 0)][string]$Command,
    [Parameter(Position = 1)][string]$Module
)

$ErrorActionPreference = 'Stop'
Set-Location $PSScriptRoot

function Show-Usage {
    @'
usage:
  ./run.ps1 test [NN]   run a module's unit tests (all modules if NN omitted)
  ./run.ps1 status      show which modules are solved
  ./run.ps1 reset NN    restore a module's stubs so you can do it again
'@ | Write-Host
}

function Get-ModulePath([string]$Number) {
    $match = Get-ChildItem -Path 'modules' -Directory -Filter "$Number-*" | Select-Object -First 1
    if (-not $match) { Write-Error "No module numbered $Number."; exit 1 }
    return $match.FullName
}

switch ($Command) {
    'test' {
        if ($Module) { dotnet test (Join-Path (Get-ModulePath $Module) 'tests/UnitTests') }
        else {
            Get-ChildItem -Path 'modules' -Directory | ForEach-Object {
                dotnet test (Join-Path $_.FullName 'tests/UnitTests')
            }
        }
    }
    'status' {
        New-Item -ItemType Directory -Force -Path artifacts | Out-Null
        Remove-Item artifacts/*.trx -ErrorAction SilentlyContinue
        Get-ChildItem -Path 'modules' -Directory | ForEach-Object {
            dotnet test (Join-Path $_.FullName 'tests/UnitTests') `
                --report-trx --report-trx-filename "$($_.Name).trx" --results-directory artifacts
        }
        dotnet run --project tools/Training.Audit -- status --trx artifacts
    }
    'reset' {
        if (-not $Module) { Show-Usage; exit 2 }
        $target = Join-Path (Get-ModulePath $Module) 'src/Exercises'
        $relative = Resolve-Path -Relative $target
        $outside = git status --porcelain -- . ":(exclude)$relative"
        if ($outside) {
            Write-Error "You have uncommitted changes outside $relative. Commit or stash them first."
            exit 1
        }
        Write-Host "About to discard your work in ${relative}:"
        git status --porcelain -- $relative
        $confirm = Read-Host "Type the module number again to confirm"
        if ($confirm -ne $Module) { Write-Host 'Cancelled.'; exit 1 }
        git checkout HEAD -- $relative
        Write-Host "Reset $relative."
    }
    default { Show-Usage; exit 2 }
}
```

- [ ] **Step 6: Commit**

```bash
git add run.sh run.ps1
git commit -m "Add the learner entry point: test, status, reset

reset makes the material re-practisable and refuses to run when there is
uncommitted work outside the module being reset."
```

---

## Task 10: CI and the pre-commit hook

**Files:**
- Create: `.github/workflows/ci.yml`, `tools/hooks/pre-commit`, `tools/install-hooks.sh`

**Interfaces:**
- Consumes: every audit subcommand, `run.sh`.
- Produces: the six named jobs — `format`, `solvable`, `stubs-are-empty`, `audit`, `integration`, `docs`.

- [ ] **Step 1: Write the CI workflow**

Create `.github/workflows/ci.yml`:

```yaml
name: CI

on:
  push:
    branches: [main]
  pull_request:

env:
  DOTNET_NOLOGO: "true"
  DOTNET_SKIP_FIRST_TIME_EXPERIENCE: "true"

jobs:
  format:
    name: format and analysers
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v5
      - uses: actions/setup-dotnet@v5
        with:
          global-json-file: global.json
      - run: dotnet restore
      - name: Verify formatting
        run: dotnet format --verify-no-changes --no-restore
      - name: Build with warnings as errors
        run: dotnet build --no-restore

  solvable:
    name: every exercise is solvable
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v5
      - uses: actions/setup-dotnet@v5
        with:
          global-json-file: global.json
      - name: Run every module against its reference solutions
        # This is the job that makes the repo trustworthy: it proves each
        # published exercise can actually be solved as written.
        run: |
          for project in modules/*/tests/UnitTests; do
            echo "::group::$project"
            dotnet test "$project" -p:UseSolutions=true
            echo "::endgroup::"
          done

  stubs-are-empty:
    name: no answer left in a stub
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v5
      - uses: actions/setup-dotnet@v5
        with:
          global-json-file: global.json
      - name: Run against the stubs and collect reports
        run: |
          mkdir -p artifacts
          for project in modules/*/tests/UnitTests; do
            name="$(basename "$(dirname "$(dirname "$project")")")"
            # Failure is the expected outcome here.
            dotnet test "$project" \
              --report-trx --report-trx-filename "$name.trx" \
              --results-directory artifacts || true
          done
      - name: Assert every exercise test class has at least one failure
        run: dotnet run --project tools/Training.Audit -- stub-leak --trx artifacts

  audit:
    name: repository invariants
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v5
      - uses: actions/setup-dotnet@v5
        with:
          global-json-file: global.json
      - name: Test the audit tool itself
        run: dotnet test tools/Training.Audit.Tests
      - name: Test the scaffolder
        run: dotnet test tools/Training.Scaffold.Tests
      - name: Pairs, API parity, guide anatomy and word count
        run: dotnet run --project tools/Training.Audit -- all

  integration:
    name: integration tier
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v5
      - uses: actions/setup-dotnet@v5
        with:
          global-json-file: global.json
      - name: Run the Docker-backed tier against real PostgreSQL
        run: |
          shopt -s nullglob
          projects=(modules/*/tests/IntegrationTests)
          if [ ${#projects[@]} -eq 0 ]; then
            echo "No integration projects yet."
            exit 0
          fi
          for project in "${projects[@]}"; do
            echo "::group::$project"
            dotnet test "$project" -p:UseSolutions=true
            echo "::endgroup::"
          done
```

- [ ] **Step 2: Write the pre-commit hook**

Create `tools/hooks/pre-commit`:

```bash
#!/usr/bin/env bash
# Pre-commit gate. Deliberately fast: formatting on staged files, then the
# invariant checks. The full test suite belongs in CI, not in your way.
set -euo pipefail

staged_cs="$(git diff --cached --name-only --diff-filter=ACM -- '*.cs')"

if [[ -n "$staged_cs" ]]; then
  echo "pre-commit: checking formatting"
  dotnet format --verify-no-changes --include $staged_cs
fi

echo "pre-commit: checking repository invariants"
dotnet run --project tools/Training.Audit -- all
```

Create `tools/install-hooks.sh`:

```bash
#!/usr/bin/env bash
# Installs the repo's git hooks. Plain scripts, no package, nothing to
# re-audit for licence changes in two years.
set -euo pipefail

REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cp "$REPO_ROOT/tools/hooks/pre-commit" "$REPO_ROOT/.git/hooks/pre-commit"
chmod +x "$REPO_ROOT/.git/hooks/pre-commit"
echo "Installed .git/hooks/pre-commit"
```

- [ ] **Step 3: Verify the hook installs and runs**

Run: `chmod +x tools/install-hooks.sh tools/hooks/pre-commit && ./tools/install-hooks.sh`
Expected: `Installed .git/hooks/pre-commit`

Run: `.git/hooks/pre-commit`
Expected: the audit runs. It will report the probe module's missing `GUIDE.md` and exit non-zero — correct, and resolved in Task 12 when the probe is retired.

- [ ] **Step 4: Verify the workflow file parses**

Run: `python3 -c "import yaml,sys; yaml.safe_load(open('.github/workflows/ci.yml')); print('valid yaml')"`
Expected: `valid yaml`

- [ ] **Step 5: Commit**

```bash
git add .github/workflows/ci.yml tools/hooks tools/install-hooks.sh
git commit -m "Add CI and the pre-commit hook

Six jobs. The two that matter: solvable proves every published exercise can be
solved, stubs-are-empty proves none of them ships with the answer."
```

---

## Task 11: The site and the front door

**Files:**
- Create: `mkdocs.yml`, `requirements-docs.txt`, `.github/workflows/docs.yml`, `.github/workflows/links.yml`
- Create: `README.md`, `START-HERE.md`, `CONTRIBUTING.md`, `LICENSE`, `LICENSE-CONTENT`
- Create: `.devcontainer/devcontainer.json`
- Create: `.github/ISSUE_TEMPLATE/guide-error.yml`, `.github/ISSUE_TEMPLATE/resource.yml`, `.github/PULL_REQUEST_TEMPLATE.md`

**Interfaces:**
- Consumes: nothing.
- Produces: a buildable site and the repository's public face.

- [ ] **Step 1: Write `mkdocs.yml`**

```yaml
site_name: Recursos C# · .NET · Nova AI
site_description: A 30-module path from C# runtime semantics to senior distributed system judgement
repo_url: https://github.com/full-stack-dev-johncastrosanabria/recursos-csharp-dotnet-nova-ai
edit_uri: edit/main/
docs_dir: .

theme:
  name: material
  language: en
  features:
    - navigation.sections
    - navigation.top
    - navigation.tracking
    - navigation.indexes
    - content.code.copy
    - content.code.annotate
    - content.action.edit
    - search.highlight
    - search.suggest
    - toc.follow
  palette:
    - scheme: default
      primary: indigo
      accent: indigo
      toggle:
        icon: material/weather-night
        name: Dark mode
    - scheme: slate
      primary: indigo
      accent: indigo
      toggle:
        icon: material/weather-sunny
        name: Light mode

plugins:
  - search
  - same-dir

exclude_docs: |
  artifacts/
  docs/superpowers/
  tools/
  system/
  modules/*/src/
  modules/*/tests/
  modules/*/examples/

markdown_extensions:
  - admonition
  - attr_list
  - md_in_html
  - tables
  - toc:
      permalink: true
      toc_depth: 3
  - pymdownx.details
  - pymdownx.highlight:
      anchor_linenums: true
  - pymdownx.inlinehilite
  - pymdownx.superfences
  - pymdownx.tabbed:
      alternate_style: true

nav:
  - Home: README.md
  - Start here: START-HERE.md
  - Runtime and language semantics:
      - 01 · Type system and memory model: modules/01-type-system-and-memory/GUIDE.md
  - Contributing: CONTRIBUTING.md
```

The `nav` grows one line per module as each is built. Create `requirements-docs.txt`:

```text
mkdocs-material==9.6.24
mkdocs-same-dir==0.1.3
```

- [ ] **Step 2: Verify the site builds**

Run: `python3 -m venv .venv-docs && .venv-docs/bin/pip install -r requirements-docs.txt && .venv-docs/bin/mkdocs build --strict`

Expected: **PASS**, `INFO - Documentation built`. If `--strict` fails on a nav entry pointing at a module that does not exist yet, remove that line until Task 13 creates it.

If either pinned version has been yanked, run `.venv-docs/bin/pip index versions mkdocs-material` and pin the current release instead — do not leave the version unpinned.

- [ ] **Step 3: Write the docs workflow**

Create `.github/workflows/docs.yml`:

```yaml
name: Docs

on:
  push:
    branches: [main]
  pull_request:

permissions:
  contents: read
  pages: write
  id-token: write

concurrency:
  group: pages
  cancel-in-progress: false

jobs:
  build:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v5
      - uses: actions/setup-python@v6
        with:
          python-version: "3.13"
      - run: pip install -r requirements-docs.txt
      - run: mkdocs build --strict
      - uses: actions/upload-pages-artifact@v4
        with:
          path: site

  deploy:
    if: github.ref == 'refs/heads/main'
    needs: build
    runs-on: ubuntu-latest
    environment:
      name: github-pages
      url: ${{ steps.deployment.outputs.page_url }}
    steps:
      - id: deployment
        uses: actions/deploy-pages@v4
```

- [ ] **Step 4: Write the two licences**

`LICENSE` is the MIT licence, copyright `2026 John Castro Sanabria`, covering code.
`LICENSE-CONTENT` is the full Creative Commons Attribution 4.0 International text, covering guides, cheatsheets and link lists. Copy both verbatim from their canonical sources; do not paraphrase a licence.

- [ ] **Step 5: Write README.md**

It must state, plainly and near the top:

- What the path is and who it is for.
- **The Docker split, honestly**: roughly 85% of exercises need only the .NET SDK; the integration tier needs Docker; every module is partially reachable without it; and what a reader without Docker is missing, and why a fake would teach them something false.
- The three commands: `./run.sh test NN`, `./run.sh status`, `./run.sh reset NN`.
- That the first run is **red on purpose**.
- A dated line: `Verified against .NET 10.0.11 and PostgreSQL 18 on 2026-08-20.`
- The dual licence.

- [ ] **Step 6: Write START-HERE.md and CONTRIBUTING.md**

`START-HERE.md`: environment check (`dotnet --version` must report 10.0.2xx), clone, first test run showing red, what red means, the toolchain and **why Shouldly rather than FluentAssertions** — the licence explanation the spec assigns to this file.

`CONTRIBUTING.md`: the fixed guide anatomy, the 3,000–5,000 prose-word rule, the voice section (judgement before syntax; every section opens with when to use something and when not; real-world cases are reproducible and their costs derived), how to add a module with the scaffolder, and how to run the audit locally.

- [ ] **Step 7: Commit**

```bash
git add mkdocs.yml requirements-docs.txt .github README.md START-HERE.md CONTRIBUTING.md LICENSE LICENSE-CONTENT .devcontainer
git commit -m "Add the site, the front door and the licences

MkDocs Material with same-dir so each GUIDE.md renders on the site and reads
correctly on GitHub, with no copy step and no link drift. MIT for code,
CC BY 4.0 for content."
```

---

## Task 12: Retire the probe module

**Files:**
- Delete: `modules/00-probe/`

The probe has done its job: it proved the build gates bite, the swap works both ways, and the audit runs. Keeping it means a permanent fake module in `status`, in the site nav and in every full test run.

- [ ] **Step 1: Confirm the probe is currently proving something**

Run: `dotnet test modules/00-probe/tests/UnitTests -p:UseSolutions=true`
Expected: **PASS**. This is the last time the machinery is verified before real content arrives.

- [ ] **Step 2: Delete it**

```bash
git rm -r modules/00-probe
```

- [ ] **Step 3: Verify the audit is clean with no modules at all**

Run: `dotnet run --project tools/Training.Audit -- all`
Expected: `audit: clean`, exit code 0. With no modules, every check has nothing to complain about — and importantly, none of them crash on an empty `modules/` directory.

- [ ] **Step 4: Verify the tool tests still pass**

Run: `dotnet test tools/Training.Audit.Tests && dotnet test tools/Training.Scaffold.Tests`
Expected: **PASS** for both. The tools' own tests use temporary directories, so deleting the probe cannot affect them.

- [ ] **Step 5: Commit**

```bash
git commit -m "Retire the probe module

It proved the build gates bite, the swap works both ways and the audit runs.
Keeping it would mean a permanent fake module in status and in the site nav."
```

---

# Phase 1 — module 01, then stop

Everything from here sets the bar for the remaining twenty-nine modules, which is why work halts for review at the end of Task 18.

Namespaces for this module: `Training.Module01.Core` and `Training.Module01.Challenge`. Test namespaces mirror them under `Training.Module01.Tests`.

## Task 13: Module 01 scaffold and Core exercises 1–2

**Files:**
- Create (via scaffolder): `modules/01-type-system-and-memory/**`
- Create: `src/Exercises/Core/Money.cs`, `src/Solutions/Core/Money.cs`, `tests/UnitTests/Core/MoneyTests.cs`
- Create: `src/Exercises/Core/CustomerReference.cs`, `src/Solutions/Core/CustomerReference.cs`, `tests/UnitTests/Core/CustomerReferenceTests.cs`

**Interfaces:**
- Consumes: `ModuleTemplate.Create` from Task 8; the swap from Task 2.
- Produces:
  - `public readonly record struct Money(decimal Amount, string Currency)` with `static Money Zero(string currency)`, `Money Add(Money other)`, `Money Multiply(int quantity)`
  - `public sealed class CurrencyMismatchException(string message) : InvalidOperationException`
  - `public sealed class CustomerReference : IEquatable<CustomerReference>` with `string Region { get; }`, `int Number { get; }`, `bool Equals(CustomerReference? other)`, `override bool Equals(object? obj)`, `override int GetHashCode()`, `static bool operator ==(CustomerReference? left, CustomerReference? right)`, `static bool operator !=(CustomerReference? left, CustomerReference? right)`

- [ ] **Step 1: Scaffold the module**

Run: `dotnet run --project tools/Training.Scaffold -- new-module 01-type-system-and-memory "Type system and memory model"`
Expected: `Created modules/01-type-system-and-memory`

- [ ] **Step 2: Write the failing tests**

Create `modules/01-type-system-and-memory/tests/UnitTests/Core/MoneyTests.cs`:

```csharp
using Shouldly;
using Training.Module01.Core;

namespace Training.Module01.Tests.Core;

public sealed class MoneyTests
{
    [Fact]
    public void Two_amounts_in_the_same_currency_add()
    {
        var total = new Money(10.50m, "USD").Add(new Money(4.50m, "USD"));

        total.ShouldBe(new Money(15.00m, "USD"));
    }

    [Fact]
    public void Adding_across_currencies_is_refused_rather_than_guessed()
    {
        var usd = new Money(10m, "USD");

        Should.Throw<CurrencyMismatchException>(() => usd.Add(new Money(10m, "EUR")));
    }

    [Fact]
    public void Equal_values_are_equal_and_hash_the_same()
    {
        var left = new Money(19.99m, "USD");
        var right = new Money(19.99m, "USD");

        left.ShouldBe(right);
        left.GetHashCode().ShouldBe(right.GetHashCode());
    }

    [Fact]
    public void The_same_amount_in_a_different_currency_is_a_different_value()
    {
        new Money(10m, "USD").ShouldNotBe(new Money(10m, "EUR"));
    }

    [Fact]
    public void Works_as_a_dictionary_key()
    {
        var ledger = new Dictionary<Money, int> { [new Money(5m, "USD")] = 1 };

        ledger[new Money(5m, "USD")].ShouldBe(1);
    }

    [Fact]
    public void Is_a_value_type_so_it_never_lands_on_the_heap_on_its_own()
    {
        typeof(Money).IsValueType.ShouldBeTrue();
    }

    [Fact]
    public void Zero_is_the_additive_identity_for_its_currency()
    {
        var price = new Money(7.25m, "USD");

        Money.Zero("USD").Add(price).ShouldBe(price);
    }

    [Fact]
    public void Multiplying_by_a_quantity_scales_the_amount_only()
    {
        new Money(3m, "USD").Multiply(4).ShouldBe(new Money(12m, "USD"));
    }
}
```

Create `modules/01-type-system-and-memory/tests/UnitTests/Core/CustomerReferenceTests.cs`:

```csharp
using Shouldly;
using Training.Module01.Core;

namespace Training.Module01.Tests.Core;

public sealed class CustomerReferenceTests
{
    [Fact]
    public void Two_references_with_the_same_parts_are_equal()
    {
        new CustomerReference("EU", 42).ShouldBe(new CustomerReference("EU", 42));
    }

    [Fact]
    public void Region_comparison_ignores_case_because_the_source_system_does()
    {
        new CustomerReference("eu", 42).ShouldBe(new CustomerReference("EU", 42));
    }

    [Fact]
    public void Equal_references_hash_the_same_or_hash_sets_break()
    {
        var set = new HashSet<CustomerReference> { new("EU", 42) };

        set.Contains(new CustomerReference("eu", 42)).ShouldBeTrue();
    }

    [Fact]
    public void Survives_a_dictionary_round_trip_with_a_different_instance()
    {
        var lookup = new Dictionary<CustomerReference, string> { [new("EU", 42)] = "Ana" };

        lookup[new CustomerReference("EU", 42)].ShouldBe("Ana");
    }

    [Fact]
    public void The_equality_operator_agrees_with_Equals()
    {
        var left = new CustomerReference("EU", 42);
        var right = new CustomerReference("EU", 42);

        (left == right).ShouldBeTrue();
        (left != right).ShouldBeFalse();
    }

    [Fact]
    public void Null_is_handled_without_throwing()
    {
        CustomerReference? nothing = null;

        (nothing == null).ShouldBeTrue();
        (new CustomerReference("EU", 42) == null).ShouldBeFalse();
        new CustomerReference("EU", 42).Equals(null).ShouldBeFalse();
    }

    [Fact]
    public void A_different_number_is_a_different_customer()
    {
        new CustomerReference("EU", 42).ShouldNotBe(new CustomerReference("EU", 43));
    }
}
```

- [ ] **Step 3: Write the stubs**

Create `modules/01-type-system-and-memory/src/Exercises/Core/Money.cs`:

```csharp
namespace Training.Module01.Core;

/// <summary>Raised when two amounts in different currencies are combined.</summary>
public sealed class CurrencyMismatchException(string message) : InvalidOperationException(message);

/// <summary>
/// An amount of money in a single currency.
///
/// Exercise: make this a value with correct equality. Two instances holding the
/// same amount and the same currency must be equal, must hash the same, and must
/// work as a dictionary key. Adding across currencies must be refused, not guessed.
/// </summary>
public readonly record struct Money(decimal Amount, string Currency)
{
    public static Money Zero(string currency) => throw new NotImplementedException();

    public Money Add(Money other) => throw new NotImplementedException();

    public Money Multiply(int quantity) => throw new NotImplementedException();
}
```

Create `modules/01-type-system-and-memory/src/Exercises/Core/CustomerReference.cs`:

```csharp
namespace Training.Module01.Core;

/// <summary>
/// A reference to a customer, as the upstream system issues it: a region code
/// and a number.
///
/// Exercise: implement the full equality contract. Region comparison is
/// case-insensitive, because the upstream system is inconsistent about it — and
/// that single fact is what makes GetHashCode interesting. Satisfy Equals
/// without satisfying GetHashCode and the dictionary tests will tell you.
/// </summary>
public sealed class CustomerReference : IEquatable<CustomerReference>
{
    public CustomerReference(string region, int number)
    {
        Region = region;
        Number = number;
    }

    public string Region { get; }

    public int Number { get; }

    public bool Equals(CustomerReference? other) => throw new NotImplementedException();

    public override bool Equals(object? obj) => throw new NotImplementedException();

    public override int GetHashCode() => throw new NotImplementedException();

    public static bool operator ==(CustomerReference? left, CustomerReference? right)
        => throw new NotImplementedException();

    public static bool operator !=(CustomerReference? left, CustomerReference? right)
        => throw new NotImplementedException();
}
```

- [ ] **Step 4: Run the tests to verify they fail**

Run: `./run.sh test 01`
Expected: **FAIL**, 15 tests failed, each with `System.NotImplementedException`.

- [ ] **Step 5: Write the reference solutions**

Create `modules/01-type-system-and-memory/src/Solutions/Core/Money.cs`:

```csharp
namespace Training.Module01.Core;

/// <summary>Raised when two amounts in different currencies are combined.</summary>
public sealed class CurrencyMismatchException(string message) : InvalidOperationException(message);

/// <summary>
/// An amount of money in a single currency.
///
/// `readonly record struct` gives value equality, a matching GetHashCode and a
/// sensible ToString for free, with no heap allocation. Writing all of that by
/// hand is the exercise in CustomerReference; here the point is knowing when
/// the compiler will do it correctly for you.
/// </summary>
public readonly record struct Money(decimal Amount, string Currency)
{
    public static Money Zero(string currency) => new(0m, currency);

    public Money Add(Money other)
    {
        if (!string.Equals(Currency, other.Currency, StringComparison.Ordinal))
        {
            throw new CurrencyMismatchException(
                $"Cannot add {other.Currency} to {Currency}. Convert first, explicitly.");
        }

        return this with { Amount = Amount + other.Amount };
    }

    public Money Multiply(int quantity) => this with { Amount = Amount * quantity };
}
```

Create `modules/01-type-system-and-memory/src/Solutions/Core/CustomerReference.cs`:

```csharp
namespace Training.Module01.Core;

/// <summary>
/// A reference to a customer, as the upstream system issues it.
///
/// The one rule that matters: whatever comparison Equals uses, GetHashCode must
/// use the same one. Region is compared case-insensitively, so the hash must be
/// computed case-insensitively too — otherwise "eu" and "EU" are equal but land
/// in different buckets, and the dictionary silently misses.
/// </summary>
public sealed class CustomerReference : IEquatable<CustomerReference>
{
    public CustomerReference(string region, int number)
    {
        Region = region;
        Number = number;
    }

    public string Region { get; }

    public int Number { get; }

    public bool Equals(CustomerReference? other)
        => other is not null
           && Number == other.Number
           && string.Equals(Region, other.Region, StringComparison.OrdinalIgnoreCase);

    public override bool Equals(object? obj) => Equals(obj as CustomerReference);

    public override int GetHashCode()
        => HashCode.Combine(Region.ToUpperInvariant(), Number);

    public static bool operator ==(CustomerReference? left, CustomerReference? right)
        => left is null ? right is null : left.Equals(right);

    public static bool operator !=(CustomerReference? left, CustomerReference? right)
        => !(left == right);
}
```

- [ ] **Step 6: Run the tests against the solutions**

Run: `dotnet test modules/01-type-system-and-memory/tests/UnitTests -p:UseSolutions=true`
Expected: **PASS**, 15 tests passed.

- [ ] **Step 7: Run the tests against the stubs one more time**

Run: `./run.sh test 01`
Expected: **FAIL**, 15 failed. Both directions must hold before committing.

- [ ] **Step 8: Verify API parity**

Run: `dotnet run --project tools/Training.Audit -- api`
Expected: `audit: clean` — the stub and the solution declare identical public surfaces.

- [ ] **Step 9: Commit**

```bash
git add modules/01-type-system-and-memory
git commit -m "Add module 01 Core exercises 1-2: Money and the equality contract

Money shows when the compiler writes equality correctly for you. CustomerReference
makes you write it by hand, with a case-insensitive region so GetHashCode has to
match Equals or the dictionary tests fail."
```

---

## Task 14: Core exercises 3–5

**Files:**
- Create: `Core/OrderTotals.cs`, `Core/BasketKey.cs`, `Core/ReservationWindow.cs` in both `src/Exercises` and `src/Solutions`
- Create: `tests/UnitTests/Core/OrderTotalsTests.cs`, `BasketKeyTests.cs`, `ReservationWindowTests.cs`

**Interfaces:**
- Consumes: `Money` from Task 13.
- Produces:
  - `public static class OrderTotals` with `decimal SumViaInterface(IReadOnlyList<Money> lines)` and `decimal SumWithoutAllocating(List<Money> lines)`
  - `public readonly record struct LineItem(string Sku, int Quantity)`
  - `public sealed record BasketKey(string CustomerId, IReadOnlyList<LineItem> Lines)` overriding `bool Equals(BasketKey? other)` and `int GetHashCode()`
  - `public readonly struct ReservationWindow` with `ReservationWindow(DateTimeOffset start, TimeSpan duration)`, `DateTimeOffset Start { get; }`, `TimeSpan Duration { get; }`, `DateTimeOffset End { get; }`, `ReservationWindow ExtendBy(TimeSpan extra)`, `bool Overlaps(in ReservationWindow other)`

- [ ] **Step 1: Write the failing tests**

Create `tests/UnitTests/Core/OrderTotalsTests.cs`:

```csharp
using Shouldly;
using Training.Module01.Core;

namespace Training.Module01.Tests.Core;

public sealed class OrderTotalsTests
{
    private static readonly List<Money> Lines =
    [
        new(10.00m, "USD"),
        new(4.50m, "USD"),
        new(0.50m, "USD"),
    ];

    /// <summary>
    /// Measures one call's allocations. The warm-up call matters: the first
    /// invocation pays for JIT compilation and static initialisation, which
    /// would otherwise be attributed to the method under test.
    /// </summary>
    private static long AllocatedBytes(Action action)
    {
        action();
        GC.Collect();
        GC.WaitForPendingFinalizers();

        var before = GC.GetAllocatedBytesForCurrentThread();
        action();
        return GC.GetAllocatedBytesForCurrentThread() - before;
    }

    [Fact]
    public void Both_versions_produce_the_same_total()
    {
        OrderTotals.SumWithoutAllocating(Lines).ShouldBe(15.00m);
        OrderTotals.SumViaInterface(Lines).ShouldBe(15.00m);
    }

    [Fact]
    public void Iterating_through_the_interface_allocates()
    {
        // foreach over IReadOnlyList<T> boxes List<T>'s struct enumerator.
        AllocatedBytes(() => OrderTotals.SumViaInterface(Lines)).ShouldBeGreaterThan(0);
    }

    [Fact]
    public void Iterating_the_concrete_list_allocates_nothing()
    {
        AllocatedBytes(() => OrderTotals.SumWithoutAllocating(Lines)).ShouldBe(0);
    }

    [Fact]
    public void An_empty_order_totals_zero()
    {
        OrderTotals.SumWithoutAllocating([]).ShouldBe(0m);
    }
}
```

Create `tests/UnitTests/Core/BasketKeyTests.cs`:

```csharp
using Shouldly;
using Training.Module01.Core;

namespace Training.Module01.Tests.Core;

public sealed class BasketKeyTests
{
    private static BasketKey Basket() => new("cus_17", [new LineItem("SKU-1", 2), new LineItem("SKU-2", 1)]);

    [Fact]
    public void Two_structurally_identical_baskets_are_equal()
    {
        Basket().ShouldBe(Basket());
    }

    [Fact]
    public void Identical_baskets_hash_the_same()
    {
        Basket().GetHashCode().ShouldBe(Basket().GetHashCode());
    }

    [Fact]
    public void The_idempotency_cache_hits_on_a_retry()
    {
        // This is the whole module in one test. A retried checkout builds an
        // equal-but-not-identical key; if the cache misses, the customer is
        // charged twice.
        var cache = new Dictionary<BasketKey, string> { [Basket()] = "charge_001" };

        cache.TryGetValue(Basket(), out var chargeId).ShouldBeTrue();
        chargeId.ShouldBe("charge_001");
    }

    [Fact]
    public void A_different_quantity_is_a_different_basket()
    {
        var other = new BasketKey("cus_17", [new LineItem("SKU-1", 3), new LineItem("SKU-2", 1)]);

        Basket().ShouldNotBe(other);
    }

    [Fact]
    public void A_different_customer_is_a_different_basket()
    {
        Basket().ShouldNotBe(new BasketKey("cus_18", [new LineItem("SKU-1", 2), new LineItem("SKU-2", 1)]));
    }

    [Fact]
    public void Line_order_is_part_of_the_key()
    {
        // A deliberate design decision, not an accident: see the guide. Treating
        // a basket as a set is defensible, and more expensive to hash correctly.
        var reversed = new BasketKey("cus_17", [new LineItem("SKU-2", 1), new LineItem("SKU-1", 2)]);

        Basket().ShouldNotBe(reversed);
    }
}
```

Create `tests/UnitTests/Core/ReservationWindowTests.cs`:

```csharp
using Shouldly;
using Training.Module01.Core;

namespace Training.Module01.Tests.Core;

public sealed class ReservationWindowTests
{
    private static readonly DateTimeOffset Noon = new(2026, 8, 20, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void End_is_start_plus_duration()
    {
        new ReservationWindow(Noon, TimeSpan.FromHours(2)).End.ShouldBe(Noon.AddHours(2));
    }

    [Fact]
    public void Extending_returns_a_new_window_and_leaves_the_original_alone()
    {
        var original = new ReservationWindow(Noon, TimeSpan.FromHours(1));

        var extended = original.ExtendBy(TimeSpan.FromHours(1));

        extended.Duration.ShouldBe(TimeSpan.FromHours(2));
        original.Duration.ShouldBe(TimeSpan.FromHours(1));
    }

    [Fact]
    public void Overlapping_windows_are_detected()
    {
        var first = new ReservationWindow(Noon, TimeSpan.FromHours(2));
        var second = new ReservationWindow(Noon.AddHours(1), TimeSpan.FromHours(2));

        first.Overlaps(second).ShouldBeTrue();
    }

    [Fact]
    public void Touching_windows_do_not_overlap()
    {
        var first = new ReservationWindow(Noon, TimeSpan.FromHours(1));
        var second = new ReservationWindow(Noon.AddHours(1), TimeSpan.FromHours(1));

        first.Overlaps(second).ShouldBeFalse();
    }

    [Fact]
    public void Overlap_is_symmetric()
    {
        var first = new ReservationWindow(Noon, TimeSpan.FromHours(2));
        var second = new ReservationWindow(Noon.AddHours(1), TimeSpan.FromHours(2));

        first.Overlaps(second).ShouldBe(second.Overlaps(first));
    }
}
```

- [ ] **Step 2: Write the stubs**

Create `src/Exercises/Core/OrderTotals.cs`:

```csharp
namespace Training.Module01.Core;

/// <summary>
/// Exercise: SumViaInterface below is given to you and it allocates. Write
/// SumWithoutAllocating so it produces the same total while allocating nothing.
/// The test measures it — you cannot talk your way past this one.
/// </summary>
public static class OrderTotals
{
    public static decimal SumViaInterface(IReadOnlyList<Money> lines)
    {
        var total = 0m;
        foreach (var line in lines)
        {
            total += line.Amount;
        }

        return total;
    }

    public static decimal SumWithoutAllocating(List<Money> lines) => throw new NotImplementedException();
}
```

Create `src/Exercises/Core/BasketKey.cs`:

```csharp
namespace Training.Module01.Core;

public readonly record struct LineItem(string Sku, int Quantity);

/// <summary>
/// The key an idempotency cache uses to recognise a retried checkout.
///
/// Exercise: a record gives you value equality for free, and here that free
/// equality is wrong — Lines is a reference, so two structurally identical
/// baskets compare unequal, the cache misses, and the customer is charged
/// twice. Override both members so structurally equal baskets are equal.
/// Whatever Equals compares, GetHashCode must agree with.
/// </summary>
public sealed record BasketKey(string CustomerId, IReadOnlyList<LineItem> Lines)
{
    public bool Equals(BasketKey? other) => throw new NotImplementedException();

    public override int GetHashCode() => throw new NotImplementedException();
}
```

Create `src/Exercises/Core/ReservationWindow.cs`:

```csharp
namespace Training.Module01.Core;

/// <summary>
/// A window during which stock is held for an order.
///
/// Exercise: a readonly struct that cannot be mutated in place. ExtendBy
/// returns a new window rather than changing this one, and Overlaps takes its
/// argument by `in` so no defensive copy is made. The examples folder shows
/// what happens when a struct is mutable and you forget.
/// </summary>
public readonly struct ReservationWindow
{
    public ReservationWindow(DateTimeOffset start, TimeSpan duration) => throw new NotImplementedException();

    public DateTimeOffset Start => throw new NotImplementedException();

    public TimeSpan Duration => throw new NotImplementedException();

    public DateTimeOffset End => throw new NotImplementedException();

    public ReservationWindow ExtendBy(TimeSpan extra) => throw new NotImplementedException();

    public bool Overlaps(in ReservationWindow other) => throw new NotImplementedException();
}
```

- [ ] **Step 3: Run the tests to verify they fail**

Run: `./run.sh test 01`
Expected: **FAIL**. The three new classes contribute 15 failures on top of Task 13's 15.

- [ ] **Step 4: Write the reference solutions**

Create `src/Solutions/Core/OrderTotals.cs`:

```csharp
namespace Training.Module01.Core;

/// <summary>
/// Two ways to add up the same numbers, one of which allocates.
///
/// `foreach` over IReadOnlyList&lt;T&gt; calls GetEnumerator through the
/// interface, which boxes List&lt;T&gt;'s struct enumerator onto the heap once
/// per call. Taking the concrete List&lt;T&gt; lets the compiler bind to the
/// struct enumerator directly and the allocation disappears.
/// </summary>
public static class OrderTotals
{
    public static decimal SumViaInterface(IReadOnlyList<Money> lines)
    {
        var total = 0m;
        foreach (var line in lines)
        {
            total += line.Amount;
        }

        return total;
    }

    public static decimal SumWithoutAllocating(List<Money> lines)
    {
        var total = 0m;
        foreach (var line in lines)
        {
            total += line.Amount;
        }

        return total;
    }
}
```

Note for the implementer: the two bodies are identical on purpose. The difference is the parameter type, and that is the entire lesson — this is the smallest possible demonstration that a type annotation, not a loop body, decides whether you allocate.

Create `src/Solutions/Core/BasketKey.cs`:

```csharp
namespace Training.Module01.Core;

public readonly record struct LineItem(string Sku, int Quantity);

/// <summary>
/// The key an idempotency cache uses to recognise a retried checkout.
///
/// The compiler-generated record equality compares Lines by reference, because
/// that is what Equals does on IReadOnlyList&lt;T&gt;. Two identical baskets
/// then hash differently, the cache misses on retry, and the charge repeats.
/// SequenceEqual and a matching hash fix it.
/// </summary>
public sealed record BasketKey(string CustomerId, IReadOnlyList<LineItem> Lines)
{
    public bool Equals(BasketKey? other)
        => other is not null
           && string.Equals(CustomerId, other.CustomerId, StringComparison.Ordinal)
           && Lines.SequenceEqual(other.Lines);

    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(CustomerId, StringComparer.Ordinal);

        foreach (var line in Lines)
        {
            hash.Add(line);
        }

        return hash.ToHashCode();
    }
}
```

Create `src/Solutions/Core/ReservationWindow.cs`:

```csharp
namespace Training.Module01.Core;

/// <summary>
/// A window during which stock is held for an order.
///
/// `readonly` on the struct tells the compiler no member mutates state, so
/// passing it by `in` needs no defensive copy. Drop the `readonly` and every
/// `in` parameter silently becomes a copy per call.
/// </summary>
public readonly struct ReservationWindow
{
    public ReservationWindow(DateTimeOffset start, TimeSpan duration)
    {
        Start = start;
        Duration = duration;
    }

    public DateTimeOffset Start { get; }

    public TimeSpan Duration { get; }

    public DateTimeOffset End => Start + Duration;

    public ReservationWindow ExtendBy(TimeSpan extra) => new(Start, Duration + extra);

    public bool Overlaps(in ReservationWindow other) => Start < other.End && other.Start < End;
}
```

- [ ] **Step 5: Run against the solutions**

Run: `dotnet test modules/01-type-system-and-memory/tests/UnitTests -p:UseSolutions=true`
Expected: **PASS**, 30 tests passed.

- [ ] **Step 6: Run against the stubs**

Run: `./run.sh test 01`
Expected: **FAIL**, 30 failed.

- [ ] **Step 7: Commit**

```bash
git add modules/01-type-system-and-memory
git commit -m "Add module 01 Core exercises 3-5

OrderTotals proves with a measurement that a parameter type decides whether you
allocate. BasketKey is the module's real-world case as an exercise: record value
equality compares a list by reference, so a retried checkout charges twice."
```

---

## Task 15: Challenge exercises 6–8

**Files:**
- Create: `Challenge/Comparisons.cs`, `Challenge/SymbolTable.cs`, `Challenge/SkuList.cs` in both source projects
- Create: `tests/UnitTests/Challenge/ComparisonsTests.cs`, `SymbolTableTests.cs`, `SkuListTests.cs`

**Interfaces:**
- Consumes: nothing from Tasks 13–14; these stand alone.
- Produces:
  - `public static class Comparisons` with `T Max<T>(T left, T right) where T : IComparable<T>` and `object MaxViaInterface(IComparable left, IComparable right)`
  - `public sealed class SymbolTable` with `string Intern(string value)` and `int Count { get; }`
  - `public readonly struct SkuList` with `SkuList(string[] skus)`, `Enumerator GetEnumerator()`, `int Count { get; }`, and nested `public struct Enumerator` with `string Current { get; }` and `bool MoveNext()`

- [ ] **Step 1: Write the failing tests**

Create `tests/UnitTests/Challenge/ComparisonsTests.cs`:

```csharp
using Shouldly;
using Training.Module01.Challenge;

namespace Training.Module01.Tests.Challenge;

public sealed class ComparisonsTests
{
    private static long AllocatedBytes(Action action)
    {
        action();
        GC.Collect();
        GC.WaitForPendingFinalizers();

        var before = GC.GetAllocatedBytesForCurrentThread();
        action();
        return GC.GetAllocatedBytesForCurrentThread() - before;
    }

    [Fact]
    public void Returns_the_larger_value()
    {
        Comparisons.Max(3, 7).ShouldBe(7);
        Comparisons.Max("apple", "banana").ShouldBe("banana");
    }

    [Fact]
    public void Returns_the_left_value_when_they_are_equal()
    {
        Comparisons.Max(5, 5).ShouldBe(5);
    }

    [Fact]
    public void The_generic_version_does_not_box_value_types()
    {
        AllocatedBytes(() => Comparisons.Max(3, 7)).ShouldBe(0);
    }

    [Fact]
    public void The_interface_version_boxes_and_that_is_the_point()
    {
        AllocatedBytes(() => Comparisons.MaxViaInterface(3, 7)).ShouldBeGreaterThan(0);
    }
}
```

Create `tests/UnitTests/Challenge/SymbolTableTests.cs`:

```csharp
using Shouldly;
using Training.Module01.Challenge;

namespace Training.Module01.Tests.Challenge;

public sealed class SymbolTableTests
{
    private static string NotInterned(string value) => new([.. value]);

    [Fact]
    public void Returns_the_same_instance_for_equal_strings()
    {
        var table = new SymbolTable();

        var first = table.Intern("USD");
        var second = table.Intern(NotInterned("USD"));

        ReferenceEquals(first, second).ShouldBeTrue();
    }

    [Fact]
    public void Reference_equality_is_therefore_safe_for_interned_symbols()
    {
        var table = new SymbolTable();

        var currencies = new[] { table.Intern("USD"), table.Intern(NotInterned("USD")) };

        (currencies[0] == currencies[1]).ShouldBeTrue();
        ReferenceEquals(currencies[0], currencies[1]).ShouldBeTrue();
    }

    [Fact]
    public void Does_not_grow_when_the_same_symbol_arrives_again()
    {
        var table = new SymbolTable();

        table.Intern("USD");
        table.Intern(NotInterned("USD"));
        table.Intern("EUR");

        table.Count.ShouldBe(2);
    }

    [Fact]
    public void Distinguishes_case_because_currency_codes_are_case_sensitive()
    {
        var table = new SymbolTable();

        table.Intern("USD");
        table.Intern("usd");

        table.Count.ShouldBe(2);
    }
}
```

Create `tests/UnitTests/Challenge/SkuListTests.cs`:

```csharp
using Shouldly;
using Training.Module01.Challenge;

namespace Training.Module01.Tests.Challenge;

public sealed class SkuListTests
{
    private static readonly string[] Skus = ["SKU-1", "SKU-2", "SKU-3"];

    private static long AllocatedBytes(Action action)
    {
        action();
        GC.Collect();
        GC.WaitForPendingFinalizers();

        var before = GC.GetAllocatedBytesForCurrentThread();
        action();
        return GC.GetAllocatedBytesForCurrentThread() - before;
    }

    private static int CountByEnumerating(SkuList list)
    {
        var seen = 0;
        foreach (var sku in list)
        {
            if (sku.Length > 0)
            {
                seen++;
            }
        }

        return seen;
    }

    [Fact]
    public void Enumerates_every_item_in_order()
    {
        var list = new SkuList(Skus);
        var collected = new List<string>();

        foreach (var sku in list)
        {
            collected.Add(sku);
        }

        collected.ShouldBe(Skus);
    }

    [Fact]
    public void Exposes_its_count()
    {
        new SkuList(Skus).Count.ShouldBe(3);
    }

    [Fact]
    public void Enumerating_allocates_nothing()
    {
        // foreach binds to the struct GetEnumerator by pattern, never through
        // IEnumerable<T>, so nothing is boxed.
        var list = new SkuList(Skus);

        AllocatedBytes(() => CountByEnumerating(list)).ShouldBe(0);
    }

    [Fact]
    public void An_empty_list_enumerates_zero_times()
    {
        CountByEnumerating(new SkuList([])).ShouldBe(0);
    }
}
```

- [ ] **Step 2: Write the stubs**

Create `src/Exercises/Challenge/Comparisons.cs`:

```csharp
namespace Training.Module01.Challenge;

/// <summary>
/// Challenge: MaxViaInterface is given and it boxes both arguments. Write Max
/// so it compares the same values without allocating. The difference is one
/// keyword and it is worth understanding rather than memorising.
/// </summary>
public static class Comparisons
{
    public static object MaxViaInterface(IComparable left, IComparable right)
        => left.CompareTo(right) >= 0 ? left : right;

    public static T Max<T>(T left, T right)
        where T : IComparable<T>
        => throw new NotImplementedException();
}
```

Create `src/Exercises/Challenge/SymbolTable.cs`:

```csharp
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
```

Create `src/Exercises/Challenge/SkuList.cs`:

```csharp
namespace Training.Module01.Challenge;

/// <summary>
/// A list of SKUs that can be iterated without allocating.
///
/// Challenge: `foreach` does not require IEnumerable&lt;T&gt;. It binds by
/// pattern to any GetEnumerator returning a type with Current and MoveNext.
/// Make that enumerator a struct and iteration allocates nothing.
/// </summary>
public readonly struct SkuList
{
    private readonly string[] _skus;

    public SkuList(string[] skus) => _skus = skus;

    public int Count => throw new NotImplementedException();

    public Enumerator GetEnumerator() => throw new NotImplementedException();

    public struct Enumerator
    {
        public string Current => throw new NotImplementedException();

        public bool MoveNext() => throw new NotImplementedException();
    }
}
```

- [ ] **Step 3: Run the tests to verify they fail**

Run: `./run.sh test 01`
Expected: **FAIL**, 42 failed.

- [ ] **Step 4: Write the reference solutions**

Create `src/Solutions/Challenge/Comparisons.cs`:

```csharp
namespace Training.Module01.Challenge;

/// <summary>
/// The same comparison, twice, at different cost.
///
/// The non-generic IComparable parameters force an int to be boxed before the
/// call can even be made. The generic constraint lets the JIT specialise the
/// method for int, calling IComparable&lt;int&gt;.CompareTo directly on the
/// value with nothing on the heap.
/// </summary>
public static class Comparisons
{
    public static object MaxViaInterface(IComparable left, IComparable right)
        => left.CompareTo(right) >= 0 ? left : right;

    public static T Max<T>(T left, T right)
        where T : IComparable<T>
        => left.CompareTo(right) >= 0 ? left : right;
}
```

Create `src/Solutions/Challenge/SymbolTable.cs`:

```csharp
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
```

Create `src/Solutions/Challenge/SkuList.cs`:

```csharp
namespace Training.Module01.Challenge;

/// <summary>
/// A list of SKUs iterated without allocating.
///
/// `foreach` binds by pattern, not by interface: the compiler looks for a
/// GetEnumerator whose result has Current and MoveNext, and only falls back to
/// IEnumerable&lt;T&gt; if there is none. A struct enumerator therefore never
/// touches the heap.
/// </summary>
public readonly struct SkuList
{
    private readonly string[] _skus;

    public SkuList(string[] skus) => _skus = skus;

    public int Count => _skus.Length;

    public Enumerator GetEnumerator() => new(_skus);

    public struct Enumerator
    {
        private readonly string[] _skus;
        private int _index;

        internal Enumerator(string[] skus)
        {
            _skus = skus;
            _index = -1;
        }

        public readonly string Current => _skus[_index];

        public bool MoveNext() => ++_index < _skus.Length;
    }
}
```

- [ ] **Step 5: Run against the solutions**

Run: `dotnet test modules/01-type-system-and-memory/tests/UnitTests -p:UseSolutions=true`
Expected: **PASS**, 42 tests passed.

- [ ] **Step 6: Verify API parity picked up the nested enumerator**

Run: `dotnet run --project tools/Training.Audit -- api`
Expected: `audit: clean`. If the `Enumerator` differs — the solution declares an `internal` constructor the stub does not — the check will say so. `internal` members are not part of the public surface, so this must stay clean.

- [ ] **Step 7: Commit**

```bash
git add modules/01-type-system-and-memory
git commit -m "Add module 01 Challenge exercises 6-8

Boxing under a generic constraint, a symbol table that does not leak the way
string.Intern does, and a struct enumerator that foreach binds to by pattern."
```

---

## Task 16: The three runnable examples

**Files:**
- Create: `examples/EqualitySurprises/{EqualitySurprises.csproj,Program.cs}`
- Create: `examples/BoxingCosts/{BoxingCosts.csproj,Program.cs}`
- Create: `examples/BasketBug/{BasketBug.csproj,Program.cs}`

**Interfaces:**
- Consumes: nothing. Each example is standalone by design — it must run from a fresh clone with no exercise solved.
- Produces: three console programs, each runnable with `dotnet run --project modules/01-type-system-and-memory/examples/<name>`.

Each `.csproj` is minimal, since `Directory.Build.props` supplies the framework:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
  </PropertyGroup>
</Project>
```

- [ ] **Step 1: Write EqualitySurprises**

`Program.cs` prints a table comparing, for the same pair of values: `==`, `Equals`, `ReferenceEquals` and `GetHashCode` equality, across four cases — two `class` instances with identical fields, two `record` instances, two `struct` values, and two `string` values built differently. The reader sees at a glance that four comparisons that "mean equal" disagree with one another.

- [ ] **Step 2: Write BoxingCosts**

`Program.cs` runs each of the six boxing sites named in the guide, measuring with `GC.GetAllocatedBytesForCurrentThread()` and printing bytes per operation, with a warm-up pass first. It ends with the same loop written to avoid boxing and prints the difference.

- [ ] **Step 3: Write BasketBug — the real-world case, reproduced**

`Program.cs` defines both a broken and a fixed basket key inline, then:

1. Builds a checkout, caches the charge under the broken key.
2. Retries the checkout with an equal-but-new key, shows the cache **misses**, and prints that a second charge would be issued.
3. Repeats with the fixed key, shows the cache **hits**, and prints that the retry is correctly recognised.
4. Prints the cost arithmetic with every input named: retry rate, orders per day, average basket, chargeback fee — and states that the reader should substitute their own figures.

This program is what makes the guide's real-world case a claim the reader can check rather than a story.

- [ ] **Step 4: Verify all three run**

```bash
for example in EqualitySurprises BoxingCosts BasketBug; do
  dotnet run --project "modules/01-type-system-and-memory/examples/$example"
done
```

Expected: all three exit 0 and print their output. `BasketBug` must visibly show the miss and then the hit.

- [ ] **Step 5: Commit**

```bash
git add modules/01-type-system-and-memory/examples
git commit -m "Add module 01 examples

BasketBug reproduces the guide's real-world case: the reader runs the double
charge, then runs the fix. A case you can execute is practice; a case you can
only read is a story."
```

---

## Task 17: Write GUIDE.md

**Files:**
- Modify: `modules/01-type-system-and-memory/GUIDE.md`

**Interfaces:**
- Consumes: every exercise from Tasks 13–15 and every example from Task 16 — the guide must reference them by their real names and real commands.
- Produces: the voice and depth standard for the remaining 29 modules.

Section budget, totalling 3,500–4,500 prose words:

| Section | Words | Must contain |
|---|---|---|
| Before you start | 100–150 | prerequisites, 3–4 hours, and the skip-ahead line pointing at `Challenge/` with the specific test: can you explain why `record struct` equality differs from `class` equality and name three places boxing hides? |
| Objectives | 80–120 | six outcomes, in verbs |
| Sections (eight) | 2,600–3,400 | each opens with when to use the thing and when not |
| Real-world case | 400–550 | the `BasketKey` double charge; names the mechanism, the detection gap, and cost arithmetic over named assumptions; links to `examples/BasketBug` |
| Exercises | 200–300 | all eight, each with its exact `./run.sh test 01` or `dotnet test --filter-class` command |
| Summary | 120–180 | what changed in the reader's head, not a section list |
| Review questions | 250–350 | eight questions, answers in `<details>`, ending with the roadmap tell verbatim |
| Resources | 120–200 | verified links only, one line each on why it is worth the time |

The eight sections, each opening with judgement rather than syntax:

1. Values and references — what the variable holds. When a struct is right; when it is a premature optimisation that makes copies worse.
2. Why "stack and heap" is the wrong mental model to lean on, and the three places it does matter.
3. `struct`, `readonly struct`, `ref struct` — and the defensive copy you pay for when `readonly` is missing.
4. The six places boxing hides: interface dispatch on a struct, assignment to `object`, non-generic collections, `params object[]`, `IComparable` without the generic constraint, and LINQ over value types.
5. The `==` / `Equals` / `GetHashCode` / `IEquatable<T>` contract, and what breaks when you satisfy one and not the others.
6. Records, and the three times value equality is wrong: a mutable member, a collection member, an inherited record.
7. Strings and interning — what `string.Intern` costs, and why the pool is never collected.
8. Mutability, and `readonly` fields that are not.

- [ ] **Step 1: Write the guide**

Write it to the section budget above. Every section opens with when to reach for the thing and when not — judgement before syntax, with no exceptions.

- [ ] **Step 2: Verify the anatomy and word count**

Run: `dotnet run --project tools/Training.Audit -- guides`
Expected: `audit: clean`. If it reports fewer than 3,000 prose words, the module is incomplete: go back to the source and find what is missing. Do not pad — the reviewer will see it, and padding is the failure the gate exists to catch.

- [ ] **Step 3: Verify every command in the guide actually works**

Run each command the guide tells the reader to run, verbatim. A guide whose commands are wrong is worse than no guide.

- [ ] **Step 4: Add the module to the site nav**

Modify `mkdocs.yml` — confirm the `01 · Type system and memory model` entry is present under `Runtime and language semantics`.

Run: `.venv-docs/bin/mkdocs build --strict`
Expected: **PASS**.

- [ ] **Step 5: Commit**

```bash
git add modules/01-type-system-and-memory/GUIDE.md mkdocs.yml
git commit -m "Add the module 01 guide

Eight sections, each opening with when to use the thing and when not. The
real-world case is the BasketKey double charge, reproducible in examples/BasketBug
with its cost derived from named assumptions."
```

---

## Task 18: Prove the whole gate is green, then stop

**Files:** none — this task only verifies.

- [ ] **Step 1: Every exercise is solvable**

Run: `dotnet test modules/01-type-system-and-memory/tests/UnitTests -p:UseSolutions=true`
Expected: **PASS**, 42 tests passed.

- [ ] **Step 2: No answer leaked into a stub**

```bash
mkdir -p artifacts && rm -f artifacts/*.trx
dotnet test modules/01-type-system-and-memory/tests/UnitTests \
  --report-trx --report-trx-filename 01.trx --results-directory artifacts || true
dotnet run --project tools/Training.Audit -- stub-leak --trx artifacts
```

Expected: `audit: clean`. Every one of the eight exercise test classes must contain at least one failure.

- [ ] **Step 3: Every invariant holds**

Run: `dotnet run --project tools/Training.Audit -- all`
Expected: `audit: clean` — pairs, API parity, guide anatomy and word count.

- [ ] **Step 4: Formatting and analysers**

Run: `dotnet format --verify-no-changes && dotnet build`
Expected: **PASS**, 0 warnings.

- [ ] **Step 5: The learner's first experience**

```bash
./run.sh test 01
./run.sh status
```

Expected: red on 42 tests, then a status table showing `01-type-system-and-memory  0/42  not started`. This is exactly what a new team member sees on their first clone, and it must be legible.

- [ ] **Step 6: The site builds**

Run: `.venv-docs/bin/mkdocs build --strict`
Expected: **PASS**.

- [ ] **Step 7: STOP**

Do not begin module 02. Report to the user:

- the word count of `GUIDE.md`,
- the exercise count and how it splits Core/Challenge,
- every verification command above with its actual result,
- anything that felt forced, especially in the real-world case.

Module 01 sets the voice, the depth and the exercise difficulty for the other twenty-nine. Building them against an unreviewed bar means rebuilding them.

---

# Phase 2 — modules 02–30

**This phase does not begin until module 01 has been reviewed and approved.**

Writing twenty-nine detailed task sets now would mean writing them against a bar nobody has agreed to. Module 01's review will change the voice, the exercise difficulty and the shape of a real-world case, and every one of those changes would have to be applied twenty-nine times. So this phase is specified as a **repeatable process plus a per-module manifest**, and the detailed tasks for each module are generated from the approved module 01 at the time that module is built.

## The per-module process

Each module is one subagent, one commit, and always the same eleven steps:

1. `dotnet run --project tools/Training.Scaffold -- new-module <slug> "<title>"`
2. Write the module's tests first — 5–8 exercises, split Core and Challenge.
3. Write the stubs. Every one throws `NotImplementedException`.
4. `./run.sh test NN` → must be **red**, with every test failing on `NotImplementedException`.
5. Write the reference solutions.
6. `dotnet test modules/<slug>/tests/UnitTests -p:UseSolutions=true` → must be **green**.
7. `dotnet run --project tools/Training.Audit -- api` → must be clean.
8. Write the 2–3 examples, including the one that reproduces the real-world case.
9. Write `GUIDE.md` to the section budget from Task 17.
10. `dotnet run --project tools/Training.Audit -- all` → must be clean, including the word count.
11. Add the module to `mkdocs.yml` nav, `mkdocs build --strict`, commit.

A module is not committed until steps 4, 6, 7 and 10 have all been observed. "It should pass" is not an observation.

## The subagent brief

Every module subagent receives, without exception:

- this plan's **Global Constraints** section, verbatim;
- the finished `modules/01-type-system-and-memory/` as the explicit bar — the guide's density, the exercise difficulty, and how a real-world case is written;
- the module's row from the manifest below;
- the eleven steps above;
- the instruction to **verify package versions against nuget.org** for anything not already in `Directory.Packages.props`, and to report rather than guess when a version cannot be confirmed.

## Module manifest

Every module's real-world case is named here so no subagent invents one. Each is a documented, mechanically reproducible failure — the module's `examples/` folder must run it.

| # | Slug · title | Real-world case (the bug the module exists to prevent) | Infra |
|---|---|---|---|
| 02 | `object-lifetime-gc-and-disposal` | A static dictionary caching orders by id, with no eviction. Memory climbs steadily while the GC runs normally, because nothing is garbage — it is all reachable. | — |
| 03 | `async-await-and-the-thread-pool` ⚓ | `.Result` on a task in a request path. Under load the thread pool starves, and a timeout appears three layers away from the blocking call. | — |
| 04 | `linq-and-the-modern-language-surface` | A query that passes its unit tests against an in-memory list and hammers the database in production, because `AsEnumerable` moved the filter client-side. | — |
| 05 | `host-configuration-and-options` | A feature flag changed in configuration at runtime and the service kept the old value: `IOptions<T>` was captured at startup where `IOptionsMonitor<T>` was needed. | — |
| 06 | `dependency-injection` | A scoped `DbContext` captured by a singleton. It surfaces days later as "a second operation was started on this context" under concurrency, not at startup. | — |
| 07 | `the-middleware-pipeline` | Authentication registered after routing. Authorization stops rejecting anything, and every endpoint is open — with no error in the logs. | — |
| 08 | `the-http-surface` | `new HttpClient()` per request exhausts sockets under load; the "fix" of a static instance then pins a stale DNS entry through a failover. | — |
| 09 | `sql-fluency` ⚓ | A predicate wrapped in a function on the indexed column. The index is ignored, the plan shows a sequential scan, and the query degrades only as the table grows. | ✔ |
| 10 | `transactions-and-concurrency` | Two concurrent stock decrements under READ COMMITTED. Both read 10, both write 9, one sale vanishes — and the oversell is discovered at fulfilment. | ✔ |
| 11 | `ef-core-internals` | A projection loop triggering lazy loads: 200 queries where one belonged. Then a stale read straight after a successful save, from the identity map. | ✔ |
| 12 | `caching-and-read-paths` | A hot catalogue key expires and 500 concurrent requests reach the database at once. The cache was protecting the database; its expiry became the outage. | ✔ |
| 13 | `oop-and-solid-applied` | An interface added over a repository purely "for testability". The tests now assert against a mock of the database and pass while the query is wrong. | — |
| 14 | `patterns-in-dotnet-idiom` | A factory class that is a constructor with extra steps, wrapping a service the DI container already builds. Three files to change a parameter. | — |
| 15 | `layered-architecture-and-ddd` | An aggregate drawn around the whole order plus its shipments. Every shipment update contends on the order row, and throughput collapses. | — |
| 16 | `cqrs` | A read model served from the same transaction as the write, so a "CQRS" system still shows stale data — the split was in the folder names only. | — |
| 17 | `messaging-and-async-integration` ⚓ | At-least-once delivery redelivers an order-placed message. The consumer is not idempotent, so a second shipment goes out. | ✔ |
| 18 | `distributed-data-consistency` ⚓ | A saga that assumes every step compensates. Payment refunds; the shipment does not un-ship. The pivot transaction was never identified. | ✔ |
| 19 | `resilience-and-observability` | Retries without jitter across every client at once. The retry storm turns a degraded dependency into a full outage — the policy amplified the failure. | ✔ |
| 20 | `security-and-identity` | A JWT validated for signature and expiry but not audience. A valid token minted for a different service is accepted as authorisation. | — |
| 21 | `performance-engineering` | A hot path optimised for allocations while a single database round trip dominated the endpoint. Measured after, not before. | — |
| 22 | `concurrency-beyond-async` | Unbounded fan-out exhausts the connection pool. Queue depth grows, more consumers are added, and the contention gets worse rather than better. | — |
| 23 | `system-design-under-constraints` | A queue chosen in the first five minutes, before anyone established the write rate. The bottleneck was the database, and the queue moved it out of sight. | — |
| 24 | `data-at-scale` | Partitioning by customer id when one customer is 40% of volume. One partition saturates while the others idle. | ✔ |
| 25 | `evolutionary-architecture-zero-downtime` | A column renamed in a single deploy. The new code works, the rollback path does not, and the outage is discovered at the moment you most need to roll back. | ✔ |
| 26 | `inherited-and-legacy-systems` ♻ | A refactor performed without characterization tests changes a rounding behaviour that invoicing quietly depended on. | — |
| 27 | `production-ownership` ♻ | Alerting on CPU rather than on checkout success rate. Checkout is broken for forty minutes with every dashboard green. | — |
| 28 | `cost-and-capacity` ♻ | A per-request call to a metered API added to a hot endpoint. It works correctly and triples the monthly bill, because unit cost per request was never computed. | — |
| 29 | `threat-modeling-at-design-time` | One connection string with full rights shared by every service. Compromising the notification service yields the entire orders database. | — |
| 30 | `technical-influence` ♻ | A design document that records the decision but not the rejected alternatives. The same argument is re-litigated every quarter because nobody can reconstruct why. | — |

The four recast modules (♻) follow the exercise designs in spec §6.1. Module 30's guide must state in its header that its tests cover only the mechanical half — fitness functions, the ADR linter, the design-doc review — and that the ADR writing is rubric-graded, not CI-graded.

## Tier checkpoints

After the last module of each tier, the corresponding `system/checkpoint-N-*` is built from the modules just finished, reusing their examples rather than adding new material. Checkpoint 3 onward carries explicit EF Core migrations; `EnsureCreated` appears nowhere.

---

# Phase 3 — publish

- [ ] Confirm all six CI jobs green on `main`.
- [ ] `gh repo create full-stack-dev-johncastrosanabria/recursos-csharp-dotnet-nova-ai --public --source . --push` — **this is an outward-facing action; confirm with the user before running it.**
- [ ] Enable GitHub Pages from Actions; verify the deployed site.
- [ ] Set the repository description and topics.
- [ ] Final pass on `README.md`: the dated verification line, the Docker split, the three commands.

---

## Self-review

**Spec coverage.** Walked every spec section against a task:

| Spec section | Task |
|---|---|
| §2 verified baseline | Task 1 (`global.json`, `Directory.Packages.props`) |
| §4.1 layout | Tasks 1, 8, 11 |
| §4.2 swap mechanism | Task 2 |
| §4.3 three audit checks | Tasks 3, 4, 5 |
| §4.4 two test tiers | Task 2 (unit), Task 10 `integration` job; per-module integration projects in Phase 2 |
| §4.5 quality gates + four-rule relaxation | Task 1, Task 8 (`StubRelaxation`) |
| §4.6 commercial licences | Global Constraints; enforced by `Directory.Packages.props` |
| §4.7 CI, six jobs | Task 10 |
| §4.8 docs site | Task 11 |
| §4.9 learner's loop | Tasks 7, 9 |
| §5 domain, checkpoints | Tasks 13–16 use order-to-cash; checkpoints in Phase 2 |
| §6 thirty modules | Phase 2 manifest |
| §7 guide anatomy | Task 5 enforces it, Task 17 writes the first one |
| §7.1 real-world case policy | Task 16 step 3, Phase 2 manifest |
| §8 module 01 | Tasks 13–18 |
| §9 build phases | This document's structure |

Two gaps found and closed while reviewing: the `docs` CI job lived only in `docs.yml` and was not named in `ci.yml` — noted in Task 11 rather than duplicated; and `run.ps1` originally passed a glob to `dotnet test`, which takes a single project — corrected to iterate.

**Placeholder scan.** No `TBD`, `TODO`, "similar to Task N", or "add error handling". Task 17 specifies a guide by section budget and required content rather than by prose, which is a writing brief, not a placeholder — the acceptance check in step 2 is mechanical.

**Type consistency.** Checked every name across task boundaries: `AuditFinding(Check, Path, Message)` is constructed identically in `PairChecker`, `ApiSurfaceChecker`, `GuideAnatomyChecker` and `StubLeakChecker`. `TrxReport.Load` is used by `StubLeakChecker`, `StatusReporter`, `run.sh` and CI, and accepts a file or directory in all four. `GuideAnatomyChecker.RequiredSections` is consumed by `ModuleTemplate.Create` and asserted in `ModuleTemplateTests`. `RepoLayout.ExerciseCounterpart` / `SolutionCounterpart` take `(moduleDirectory, testFilePath)` at both definition and call site. `IsTrainingTestProject` is set by `ModuleTemplate` and read by `Directory.Build.targets`.
