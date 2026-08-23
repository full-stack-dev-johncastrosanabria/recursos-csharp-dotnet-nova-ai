// The module's real-world case, counted rather than described.
//
// This runs against SQLite in memory: a real relational database, real SQL, no
// server and no Docker. The dialect is not PostgreSQL, and for this question it
// does not need to be -- what is being counted is round trips, and a round trip
// costs the same wherever it goes.

using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Training.Module11.Examples;

using var connection = new SqliteConnection("Filename=:memory:");
connection.Open();
var counter = new CommandCounter();

DbContextOptions<ShopContext> Options(bool lazy)
{
    var builder = new DbContextOptionsBuilder<ShopContext>()
        .UseSqlite(connection)
        .AddInterceptors(counter);

    if (lazy)
    {
        builder.UseLazyLoadingProxies();
    }

    return builder.Options;
}

using (var db = new ShopContext(Options(false)))
{
    db.Database.EnsureCreated();
    for (var i = 1; i <= 200; i++)
    {
        db.Orders.Add(new Order
        {
            Reference = $"ORD-{i}",
            Lines = [new OrderLine { Sku = "A" }, new OrderLine { Sku = "B" }],
        });
    }

    db.SaveChanges();
}

Console.WriteLine("200 orders, 2 lines each. Total the lines, three ways.");
Console.WriteLine();
Console.WriteLine($"  {"strategy",-34}{"SQL commands",14}{"lines",8}");
Console.WriteLine("  " + new string('-', 58));

Run("lazy loading in a loop", lazy: true, db =>
    db.Orders.ToList().Sum(order => order.Lines.Count));

Run("Include", lazy: false, db =>
    db.Orders.Include(order => order.Lines).ToList().Sum(order => order.Lines.Count));

Run("projection", lazy: false, db =>
    db.Orders.Select(order => order.Lines.Count).ToList().Sum());

Console.WriteLine();
Console.WriteLine("All three answers are identical. One of them asked the database 201");
Console.WriteLine("times, and nothing in the C# said so -- `order.Lines` is a property");
Console.WriteLine("access, and it reads exactly like one.");
Console.WriteLine();
Console.WriteLine("Note where this hides. It is fast with the ten rows you have in");
Console.WriteLine("development, and every one of those queries is individually fast in");
Console.WriteLine("production too, so nothing shows up as a slow query. What you get is a");
Console.WriteLine("page that takes four seconds while the database reports no problem, and");
Console.WriteLine("a profile that blames a loop nobody thought was doing I/O.");
Console.WriteLine();
Console.WriteLine("The cost also scales with the wrong thing: not with data volume, but");
Console.WriteLine("with how many rows the page shows. Growing the page size from 20 to 200");
Console.WriteLine("multiplies the round trips by ten.");

void Run(string label, bool lazy, Func<ShopContext, int> work)
{
    counter.Reset();
    using var db = new ShopContext(Options(lazy));
    var lines = work(db);
    Console.WriteLine($"  {label,-34}{counter.Count,14}{lines,8}");
}

namespace Training.Module11.Examples
{
    internal sealed class CommandCounter : DbCommandInterceptor
    {
        private int _count;

        public int Count => _count;

        public void Reset() => _count = 0;

        public override InterceptionResult<System.Data.Common.DbDataReader> ReaderExecuting(
            System.Data.Common.DbCommand command,
            CommandEventData eventData,
            InterceptionResult<System.Data.Common.DbDataReader> result)
        {
            Interlocked.Increment(ref _count);

            return result;
        }
    }

    internal sealed class ShopContext(DbContextOptions<ShopContext> options) : DbContext(options)
    {
        public DbSet<Order> Orders => Set<Order>();

        public DbSet<OrderLine> Lines => Set<OrderLine>();
    }

    public class Order
    {
        public int Id { get; set; }

        public string Reference { get; set; } = string.Empty;

        public virtual List<OrderLine> Lines { get; set; } = [];
    }

    public class OrderLine
    {
        public int Id { get; set; }

        public int OrderId { get; set; }

        public virtual Order Order { get; set; } = null!;

        public string Sku { get; set; } = string.Empty;
    }
}
