using Training.Scaffold;

if (args is not ["new-module", var slug, var title])
{
    Console.Error.WriteLine("""
        usage: dotnet run --project tools/Training.Scaffold -- new-module <slug> "<title>"
        example: dotnet run --project tools/Training.Scaffold -- new-module 07-the-middleware-pipeline "The middleware pipeline"
        """);
    return 2;
}

if (!ModuleTemplate.TryParseModuleNumber(slug, out var number))
{
    Console.Error.WriteLine(
        $"Slug must start with a two-digit module number followed by '-': '{slug}'");
    return 2;
}

try
{
    ModuleTemplate.Create(Directory.GetCurrentDirectory(), slug, title, number);
}
catch (ModuleTemplateException ex)
{
    Console.Error.WriteLine(ex.Message);
    return 2;
}

Console.WriteLine($"Created modules/{slug}");
return 0;
