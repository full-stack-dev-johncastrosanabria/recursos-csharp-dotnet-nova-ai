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

                foreach (var member in declaration.Members)
                {
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
