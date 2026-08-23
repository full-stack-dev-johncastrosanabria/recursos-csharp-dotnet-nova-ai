// Every question of the form "what does this LINQ actually do" has a mechanical
// answer, available where you are writing it and costing nothing.
//
// ToQueryString() returns the SQL without executing it and without a database.
// The connection string below points nowhere; the provider supplies the
// dialect, and that is all it needs.

using Microsoft.EntityFrameworkCore;
using Training.Module11.Examples;

using var db = new ShopContext();

Show("no include", db.Orders);
Show("Include(Lines)", db.Orders.Include(o => o.Lines));
Show("Include(Lines) + Include(Payments)", db.Orders.Include(o => o.Lines).Include(o => o.Payments));
Show("...AsSplitQuery()", db.Orders.Include(o => o.Lines).Include(o => o.Payments).AsSplitQuery());
Show("projection", db.Orders.Select(o => new { o.Id, Lines = o.Lines.Count }));
Show("Where(Reference.ToUpper() == x)", ByUpper(db, "ORD-1"));

Console.WriteLine("Read the third one against the fourth. Two collection includes in a");
Console.WriteLine("single statement is two LEFT JOINs, and a join of two independent");
Console.WriteLine("one-to-manys returns their PRODUCT: every line paired with every");
Console.WriteLine("payment. 100 orders with 20 lines and 10 payments is 20,000 rows on the");
Console.WriteLine("wire, not 3,100. EF discards the duplicates while materialising, so the");
Console.WriteLine("object graph is right and only the wire, the memory and the clock pay.");
Console.WriteLine();
Console.WriteLine("Then read the last one. It translates perfectly -- and it wraps the");
Console.WriteLine("column in a function, which is exactly the predicate module 09 showed");
Console.WriteLine("reading 200,000 rows to return four. Nothing warns you. The LINQ is");
Console.WriteLine("idiomatic, the SQL is correct, and the index on Reference is unusable.");

// CA1862 wants string.Equals(..., StringComparison.OrdinalIgnoreCase) here,
// and for ordinary in-memory code it is right. Inside an expression tree that
// overload has no SQL translation at all, so following the advice replaces a
// working query with a runtime exception. Suppressed deliberately -- this
// example exists to show what the translatable version produces.
#pragma warning disable CA1862
static IQueryable<Order> ByUpper(ShopContext db, string reference)
    => db.Orders.Where(o => o.Reference.ToUpper() == reference);
#pragma warning restore CA1862

static void Show(string label, IQueryable query)
{
    // Drop EF's "-- @name='value'" parameter declarations before collapsing
    // the newlines. Collapse them in and the comment swallows the statement --
    // which is a real trap if you ever paste this into a SQL console.
    var lines = query.ToQueryString()
        .Replace("\r", string.Empty, StringComparison.Ordinal)
        .Split('\n')
        .Where(line => !line.TrimStart().StartsWith("--", StringComparison.Ordinal));

    var sql = string.Join(' ', lines);

    while (sql.Contains("  ", StringComparison.Ordinal))
    {
        sql = sql.Replace("  ", " ", StringComparison.Ordinal);
    }

    Console.WriteLine($"  {label}");
    Console.WriteLine($"      {sql.Trim()}");
    Console.WriteLine();
}

namespace Training.Module11.Examples
{
    internal sealed class ShopContext : DbContext
    {
        public DbSet<Order> Orders => Set<Order>();

        protected override void OnConfiguring(DbContextOptionsBuilder builder)
            => builder.UseNpgsql("Host=nowhere.invalid;Database=none;Username=none;Password=none");
    }

    public class Order
    {
        public int Id { get; set; }

        public string Reference { get; set; } = string.Empty;

        public decimal Total { get; set; }

        public List<OrderLine> Lines { get; set; } = [];

        public List<Payment> Payments { get; set; } = [];
    }

    public class OrderLine
    {
        public int Id { get; set; }

        public int OrderId { get; set; }

        public string Sku { get; set; } = string.Empty;
    }

    public class Payment
    {
        public int Id { get; set; }

        public int OrderId { get; set; }

        public decimal Amount { get; set; }
    }
}
