using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;

namespace Training.Module11.Core;

/// <summary>
/// Reading what EF Core would actually send, without executing it and without
/// a database: the provider supplies the dialect, not a connection.
/// </summary>
public static partial class GeneratedSql
{
    public static string For(IQueryable query)
        => Whitespace().Replace(query.ToQueryString(), " ").Trim();

    public static int Occurrences(string sql, string token)
    {
        var count = 0;
        var at = 0;

        while ((at = sql.IndexOf(token, at, StringComparison.OrdinalIgnoreCase)) >= 0)
        {
            count++;
            at += token.Length;
        }

        return count;
    }

    public static bool Mentions(string sql, string token)
        => sql.Contains(token, StringComparison.OrdinalIgnoreCase);

    [GeneratedRegex(@"\s+")]
    private static partial Regex Whitespace();
}
