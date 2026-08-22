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
            var exercises = RepoLayout.ExercisesDirectory(module);
            var solutions = RepoLayout.SolutionsDirectory(module);

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

                // A positional record's parameters are its public API — Amount and
                // Currency below are compiler-synthesised properties that never
                // appear in Members — but they only exist as a ParameterList, so
                // they need their own surface entry.
                if (declaration is RecordDeclarationSyntax { ParameterList: { } primaryConstructor })
                {
                    surface.Add($"{typeName}.primary-ctor{Render(primaryConstructor)}");
                }

                foreach (var member in declaration.Members)
                {
                    // A nested type is itself a member, but it is also visited as
                    // its own entry by the outer loop above (with a correctly
                    // qualified name and member-level comparison). Treating it as
                    // an ordinary member here would compare its entire source text
                    // — bodies included — instead of just its public surface.
                    if (member is BaseTypeDeclarationSyntax)
                    {
                        continue;
                    }

                    if (!IsPubliclyVisible(member.Modifiers))
                    {
                        continue;
                    }

                    surface.Add($"{typeName}.{Normalise(member)}");
                }
            }
        }

        return surface;
    }

    private static bool IsPubliclyVisible(SyntaxTokenList modifiers)
        => modifiers.Any(m => m.IsKind(SyntaxKind.PublicKeyword) || m.IsKind(SyntaxKind.ProtectedKeyword));

    /// <summary>
    /// The type's name, prefixed by every enclosing type (outermost first) and
    /// its namespace, so `Outer.Enumerator` and `Outer2.Enumerator` stay in
    /// separate buckets instead of colliding on their bare identifier.
    /// </summary>
    private static string QualifiedName(BaseTypeDeclarationSyntax type)
    {
        var namespaceName = type.Ancestors()
            .OfType<BaseNamespaceDeclarationSyntax>()
            .Select(n => n.Name.ToString())
            .FirstOrDefault() ?? string.Empty;

        var enclosingTypes = type.Ancestors()
            .OfType<TypeDeclarationSyntax>()
            .Reverse()
            .Select(t => t.Identifier.Text);

        var name = type.Identifier.Text;
        if (type is TypeDeclarationSyntax { TypeParameterList: { } parameters })
        {
            name += parameters.ToString();
        }

        var qualifiedTypeName = string.Join('.', [.. enclosingTypes, name]);

        return namespaceName.Length == 0 ? qualifiedTypeName : $"{namespaceName}.{qualifiedTypeName}";
    }

    /// <summary>
    /// A member declaration with its body, initialiser and trivia removed, so
    /// only the caller-visible signature survives. `public int Add(int a) => a + 1;`
    /// and `public int Add(int a) => throw new NotImplementedException();` normalise
    /// to the same string; renaming `a` to `b`, or dropping `static`, does not.
    ///
    /// Modifiers are compared too — `static`, `const`, `readonly`, `abstract`,
    /// `virtual`, `override`, `sealed` and the accessibility keywords are all
    /// caller-visible — except `async`, which is an implementation detail: an
    /// `async Task&lt;T&gt;` stub and a synchronous `Task&lt;T&gt;` solution present the
    /// same contract to a caller. Retained modifiers are sorted so source
    /// ordering cannot cause a spurious difference.
    ///
    /// Every syntax fragment is rendered through NormalizeWhitespace so a
    /// parameter list wrapped across lines matches the same list on one line.
    /// </summary>
    private static string Normalise(MemberDeclarationSyntax member)
    {
        var modifiers = string.Join(' ', member.Modifiers
            .Select(token => token.Text)
            .Where(text => text != "async")
            .OrderBy(text => text, StringComparer.Ordinal));

        var signature = member switch
        {
            MethodDeclarationSyntax m => $"{Render(m.ReturnType)} {m.Identifier}{Render(m.TypeParameterList)}{Render(m.ParameterList)}",
            ConstructorDeclarationSyntax c => $"ctor {Render(c.ParameterList)}",
            PropertyDeclarationSyntax p => $"{Render(p.Type)} {p.Identifier} {{ {Accessors(p.AccessorList)} }}",
            IndexerDeclarationSyntax i => $"{Render(i.Type)} this{Render(i.ParameterList)}",
            EventDeclarationSyntax e => $"event {Render(e.Type)} {e.Identifier}",
            FieldDeclarationSyntax f => $"{Render(f.Declaration.Type)} {string.Join(",", f.Declaration.Variables.Select(v => v.Identifier.Text))}",
            OperatorDeclarationSyntax o => $"operator {o.OperatorToken} {Render(o.ReturnType)}{Render(o.ParameterList)}",
            ConversionOperatorDeclarationSyntax v => $"conversion {Render(v.Type)}{Render(v.ParameterList)}",
            _ => member.WithModifiers(default).NormalizeWhitespace().ToFullString().Trim(),
        };

        return modifiers.Length == 0 ? signature : $"{modifiers} {signature}";
    }

    private static string Accessors(AccessorListSyntax? accessors)
        => accessors is null
            ? string.Empty
            : string.Join(" ", accessors.Accessors.Select(a => a.Keyword.Text + ";"));

    /// <summary>Canonical text for a syntax fragment, independent of how the source wrapped it.</summary>
    private static string Render(SyntaxNode? node)
        => node is null ? string.Empty : node.NormalizeWhitespace().ToFullString();
}
