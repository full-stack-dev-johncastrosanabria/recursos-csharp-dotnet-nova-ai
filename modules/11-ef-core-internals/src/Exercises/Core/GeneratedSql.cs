namespace Training.Module11.Core;

/// <summary>
/// Exercise: read what EF Core would actually send.
///
/// This is the single most useful habit in the module, and it costs nothing.
/// ToQueryString() returns the SQL for a query WITHOUT executing it and without
/// a database anywhere -- the provider supplies the dialect, not a connection.
/// So every question of the form "what does this LINQ do" has a mechanical
/// answer available at the point you write it, rather than a guess confirmed in
/// production three months later.
///
/// For returns that SQL with runs of whitespace collapsed to single spaces and
/// the ends trimmed, so tests can match on it. Occurrences counts a token
/// case-insensitively; Mentions is the boolean form.
/// </summary>
public static class GeneratedSql
{
    public static string For(IQueryable query) => throw new NotImplementedException();

    public static int Occurrences(string sql, string token) => throw new NotImplementedException();

    public static bool Mentions(string sql, string token) => throw new NotImplementedException();
}
