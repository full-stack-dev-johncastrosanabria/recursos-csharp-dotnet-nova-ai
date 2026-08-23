// The second half of the module's real-world case, and the more alarming half.
//
// A context hands back the first instance it ever saw for a given key. Not from
// a cache that avoids the round trip -- the round trip happens, the current row
// comes back, and EF Core discards it in favour of the object it is already
// tracking. You pay for the query and get the old answer.

using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Training.Module11.Examples;

using var connection = new SqliteConnection("Filename=:memory:");
connection.Open();
var counter = new CommandCounter();

DbContextOptions<ShopContext> Options() =>
    new DbContextOptionsBuilder<ShopContext>().UseSqlite(connection).AddInterceptors(counter).Options;

using (var setup = new ShopContext(Options()))
{
    setup.Database.EnsureCreated();
    setup.Orders.Add(new Order { Reference = "ORD-1", Total = 100m });
    setup.SaveChanges();
}

using var session = new ShopContext(Options());

var order = session.Orders.First(o => o.Reference == "ORD-1");
Console.WriteLine($"  this context loaded ORD-1                      total = {order.Total}");

using (var elsewhere = new ShopContext(Options()))
{
    var same = elsewhere.Orders.First(o => o.Reference == "ORD-1");
    same.Total = 250m;
    elsewhere.SaveChanges();
}

Console.WriteLine("  another context set it to 250 and committed");
Console.WriteLine();

counter.Reset();
var again = session.Orders.First(o => o.Reference == "ORD-1");
Console.WriteLine($"  re-queried in the first context                total = {again.Total}");
Console.WriteLine($"    same object as before?                       {ReferenceEquals(order, again)}");
Console.WriteLine($"    SQL commands that re-query actually sent:    {counter.Count}");
Console.WriteLine();

var untracked = session.Orders.AsNoTracking().First(o => o.Reference == "ORD-1");
Console.WriteLine($"  AsNoTracking()                                 total = {untracked.Total}");

session.Entry(order).Reload();
Console.WriteLine($"  Entry(order).Reload()                          total = {order.Total}");

Console.WriteLine();
Console.WriteLine("The middle line is the one to sit with. The query ran. The database");
Console.WriteLine("returned 250. EF Core threw that away and handed back the instance it");
Console.WriteLine("was already tracking, because within one context an entity has one");
Console.WriteLine("identity -- and that guarantee is what makes change tracking work at all.");
Console.WriteLine();
Console.WriteLine("So this is not a bug, and turning it off is not the answer. It is a");
Console.WriteLine("contract with a scope, and the scope is the context. It bites when a");
Console.WriteLine("context outlives the operation it was created for: a singleton DbContext,");
Console.WriteLine("a context cached on a service, one held open across a whole request that");
Console.WriteLine("both writes and then re-reads through another path.");
Console.WriteLine();
Console.WriteLine("Three ways out, in order of preference. Keep contexts short -- one per");
Console.WriteLine("unit of work, which is what AddDbContext's scoped lifetime gives you.");
Console.WriteLine("Read with AsNoTracking when you are not going to save. And when you are");
Console.WriteLine("holding an entity you know is stale, Reload it deliberately.");

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
    }

    public class Order
    {
        public int Id { get; set; }

        public string Reference { get; set; } = string.Empty;

        public decimal Total { get; set; }
    }
}
