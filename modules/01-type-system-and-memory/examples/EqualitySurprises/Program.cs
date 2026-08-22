// Four comparisons that all sound like "are these equal?", disagreeing with
// each other on the same pair of values. Run it, then read section 5 of the
// guide. Nothing here is a trick: every row is the language behaving exactly
// as specified, which is the problem.

Console.WriteLine("Four ways to ask 'are these equal?', and what each one answers.");
Console.WriteLine();
Console.WriteLine($"{"case",-34}{"==",-8}{"Equals",-9}{"ReferenceEquals",-18}{"same hash",-10}");
Console.WriteLine(new string('-', 79));

var leftClass = new PointClass(1, 2);
var rightClass = new PointClass(1, 2);
Row("two class instances, same fields", leftClass == rightClass, leftClass.Equals(rightClass),
    ReferenceEquals(leftClass, rightClass), leftClass.GetHashCode() == rightClass.GetHashCode());

var leftRecord = new PointRecord(1, 2);
var rightRecord = new PointRecord(1, 2);
Row("two record instances, same fields", leftRecord == rightRecord, leftRecord.Equals(rightRecord),
    ReferenceEquals(leftRecord, rightRecord), leftRecord.GetHashCode() == rightRecord.GetHashCode());

var leftStruct = new PointStruct(1, 2);
var rightStruct = new PointStruct(1, 2);
Row("two struct values, same fields", leftStruct == rightStruct, leftStruct.Equals(rightStruct),
    false, leftStruct.GetHashCode() == rightStruct.GetHashCode());

var literal = "USD";
var built = new string(['U', 'S', 'D']);
Row("two strings, built differently", literal == built, literal.Equals(built, StringComparison.Ordinal),
    ReferenceEquals(literal, built), literal.GetHashCode(StringComparison.Ordinal) == built.GetHashCode(StringComparison.Ordinal));

object boxedLeft = leftStruct;
object boxedRight = rightStruct;
Row("the same struct values, boxed", boxedLeft == boxedRight, boxedLeft.Equals(boxedRight),
    ReferenceEquals(boxedLeft, boxedRight), boxedLeft.GetHashCode() == boxedRight.GetHashCode());

Console.WriteLine();
Console.WriteLine("What to take from this:");
Console.WriteLine("  - The class row is the default you inherit if you write nothing: identity, not value.");
Console.WriteLine("  - The record and struct rows agree because the compiler wrote the members for you.");
Console.WriteLine("  - The string row shows == is not reference comparison for string; it is overloaded.");
Console.WriteLine("  - The boxed row is the trap: == on object is reference comparison again, so two");
Console.WriteLine("    equal values compare unequal the moment they are boxed.");

static void Row(string label, bool equalityOperator, bool equals, bool referenceEquals, bool sameHash)
    => Console.WriteLine($"{label,-34}{Yes(equalityOperator),-8}{Yes(equals),-9}{Yes(referenceEquals),-18}{Yes(sameHash),-10}");

static string Yes(bool value) => value ? "true" : "false";

internal sealed class PointClass(int x, int y)
{
    public int X { get; } = x;

    public int Y { get; } = y;
}

internal sealed record PointRecord(int X, int Y);

internal readonly record struct PointStruct(int X, int Y);
