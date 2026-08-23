using Training.Module09.Core;

namespace Training.Module09.Challenge;

/// <summary>What to do about a predicate that cannot use a plain index.</summary>
public enum Remedy
{
    CreatePlainIndex,
    CreateExpressionIndex,
    RewriteAsRange,
    NoBTreeIndexHelps,
}

/// <summary>A remedy and the statement or instruction that carries it out.</summary>
public sealed record Advice(Remedy Remedy, string Statement);

/// <summary>
/// Turning a verdict into a decision -- which is not always "add an index",
/// because an index costs something on every write for the life of the table.
/// </summary>
public static class IndexAdvice
{
    public static Advice For(string table, string predicate)
    {
        var left = Sargability.LeftHandSide(predicate);

        return Sargability.Classify(predicate) switch
        {
            SargabilityVerdict.Sargable =>
                new Advice(Remedy.CreatePlainIndex, $"CREATE INDEX ON {table} ({left});"),

            SargabilityVerdict.FunctionOnColumn or SargabilityVerdict.ArithmeticOnColumn =>
                new Advice(Remedy.CreateExpressionIndex, $"CREATE INDEX ON {table} ({left});"),

            // The one case where the query, not the schema, is at fault: a
            // half-open range uses the index that already exists.
            SargabilityVerdict.CastOnColumn =>
                new Advice(
                    Remedy.RewriteAsRange,
                    $"Rewrite as a half-open range over {left.Split("::")[0]} instead of casting it."),

            _ => new Advice(
                Remedy.NoBTreeIndexHelps,
                "A B-tree cannot serve this; consider a trigram or full-text index, or a different predicate."),
        };
    }
}
