using Microsoft.EntityFrameworkCore;

namespace Training.Module11.Core;

/// <summary>An order, with lines. Navigations are virtual so lazy loading can be switched on.</summary>
public class Order
{
    public int Id { get; set; }

    public string Reference { get; set; } = string.Empty;

    public string CustomerEmail { get; set; } = string.Empty;

    public decimal Total { get; set; }

    public virtual List<OrderLine> Lines { get; set; } = [];

    public virtual List<Payment> Payments { get; set; } = [];
}

/// <summary>A payment against an order. A second collection, which is what makes a cartesian product possible.</summary>
public class Payment
{
    public int Id { get; set; }

    public int OrderId { get; set; }

    public virtual Order Order { get; set; } = null!;

    public string Method { get; set; } = string.Empty;

    public decimal Amount { get; set; }
}

/// <summary>One line of an order.</summary>
public class OrderLine
{
    public int Id { get; set; }

    public int OrderId { get; set; }

    public virtual Order Order { get; set; } = null!;

    public string Sku { get; set; } = string.Empty;

    public int Quantity { get; set; }
}

/// <summary>
/// The model every exercise in this module queries.
///
/// Given rather than asked for: this is scaffolding, not the lesson. Two
/// details do matter. The navigations are virtual, which is what makes lazy
/// loading proxies possible and therefore what makes the module's real-world
/// case possible at all. And nothing here configures a connection that works --
/// most of this module asks what SQL EF Core WOULD send, which it will tell you
/// without a database anywhere.
/// </summary>
public class ShopContext(DbContextOptions<ShopContext> options) : DbContext(options)
{
    public DbSet<Order> Orders => Set<Order>();

    public DbSet<OrderLine> Lines => Set<OrderLine>();

    public DbSet<Payment> Payments => Set<Payment>();

    /// <summary>
    /// A context wired to PostgreSQL that is never connected to. ToQueryString
    /// needs a provider so it knows the dialect; it does not need a server.
    /// </summary>
    public static ShopContext ForSqlOnly()
        => new(new DbContextOptionsBuilder<ShopContext>()
            .UseNpgsql("Host=nowhere.invalid;Database=none;Username=none;Password=none")
            .Options);
}
