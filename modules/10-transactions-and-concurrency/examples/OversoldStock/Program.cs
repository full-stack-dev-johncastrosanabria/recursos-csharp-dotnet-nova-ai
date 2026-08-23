// The module's real-world case, raced against a real PostgreSQL.
//
// This one needs Docker, and it needs it for a reason worth stating: a race is
// an event, not a document. Module 09 could ship captured query plans in files
// because a plan is a thing you can save. There is no honest way to save a race
// -- so this starts a database, runs ten buyers at one row, and reports what
// actually happened.

using System.Data;
using Npgsql;
using Testcontainers.PostgreSql;

const int Buyers = 10;
const int Stock = 10;

await using var container = new PostgreSqlBuilder("postgres:18").Build();
Console.WriteLine("Starting PostgreSQL...");
await container.StartAsync();
var connectionString = container.GetConnectionString();

await Execute("""
    CREATE TABLE stock (sku text PRIMARY KEY, quantity int NOT NULL, version int NOT NULL DEFAULT 0);
    """);

Console.WriteLine();
Console.WriteLine($"{Buyers} buyers, {Stock} units, all arriving at once.");
Console.WriteLine();
Console.WriteLine($"  {"strategy",-40}{"sold",7}{"left",7}{"conserved?",16}");
Console.WriteLine("  " + new string('-', 71));

await Show("read-modify-write, READ COMMITTED", () => ReadModifyWrite(IsolationLevel.ReadCommitted));
await Show("read-modify-write, REPEATABLE READ", () => ReadModifyWrite(IsolationLevel.RepeatableRead));
await Show("UPDATE ... quantity = quantity - 1", AtomicDecrement);
await Show("SELECT ... FOR UPDATE, then write", ForUpdate);

Console.WriteLine();
Console.WriteLine("Row one is the bug. Every buyer read 10, every buyer wrote 9, and the last");
Console.WriteLine("write won. Ten sales are recorded and nine units of stock never existed.");
Console.WriteLine("Nothing failed: ten transactions committed successfully, and READ COMMITTED");
Console.WriteLine("did exactly what it promises. It promises you will not read uncommitted");
Console.WriteLine("data. It does not promise the row is unchanged since you read it.");
Console.WriteLine();
Console.WriteLine("Row two raises the isolation level and the problem changes shape rather");
Console.WriteLine("than going away: PostgreSQL detects the conflict and aborts the losers with");
Console.WriteLine("SQLSTATE 40001. Nothing is oversold, and almost nobody is served. That is");
Console.WriteLine("only useful with the retry loop from exercise 2 around it.");
Console.WriteLine();
Console.WriteLine("Rows three and four both serve everybody and oversell nothing, and they do");
Console.WriteLine("it differently. The atomic UPDATE has no window at all -- the read and the");
Console.WriteLine("write are one statement, so there is nothing to lose. FOR UPDATE keeps the");
Console.WriteLine("read-then-write shape and locks the row for the duration, which works and");
Console.WriteLine("serialises every buyer behind one lock.");
Console.WriteLine();
Console.WriteLine("The order to reach for them: make it one statement if you can, lock if you");
Console.WriteLine("must, and use versions when the think-time is long or spans a user.");

async Task Show(string label, Func<Task<int>> race)
{
    await Reset();
    var sold = await race();
    var left = await Quantity();
    var conserved = sold + left == Stock;

    Console.WriteLine($"  {label,-40}{sold,7}{left,7}{(conserved ? "yes" : "NO - oversold"),16}");
}

async Task<int> ReadModifyWrite(IsolationLevel level)
{
    var sold = 0;
    await Race(async () =>
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();
        await using var transaction = await connection.BeginTransactionAsync(level);

        int quantity;
        await using (var read = new NpgsqlCommand("SELECT quantity FROM stock WHERE sku='W'", connection, transaction))
        {
            quantity = (int)(await read.ExecuteScalarAsync())!;
        }

        await Task.Delay(40);            // the window every real handler has

        if (quantity <= 0)
        {
            await transaction.RollbackAsync();

            return;
        }

        await using (var write = new NpgsqlCommand("UPDATE stock SET quantity=@q WHERE sku='W'", connection, transaction))
        {
            write.Parameters.AddWithValue("q", quantity - 1);
            await write.ExecuteNonQueryAsync();
        }

        await transaction.CommitAsync();
        Interlocked.Increment(ref sold);
    });

    return sold;
}

async Task<int> AtomicDecrement()
{
    var sold = 0;
    await Race(async () =>
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(
            "UPDATE stock SET quantity = quantity - 1 WHERE sku='W' AND quantity > 0", connection);

        if (await command.ExecuteNonQueryAsync() == 1)
        {
            Interlocked.Increment(ref sold);
        }
    });

    return sold;
}

async Task<int> ForUpdate()
{
    var sold = 0;
    await Race(async () =>
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();
        await using var transaction = await connection.BeginTransactionAsync();

        int quantity;
        await using (var read = new NpgsqlCommand(
            "SELECT quantity FROM stock WHERE sku='W' FOR UPDATE", connection, transaction))
        {
            quantity = (int)(await read.ExecuteScalarAsync())!;
        }

        if (quantity <= 0)
        {
            await transaction.RollbackAsync();

            return;
        }

        await using (var write = new NpgsqlCommand("UPDATE stock SET quantity=@q WHERE sku='W'", connection, transaction))
        {
            write.Parameters.AddWithValue("q", quantity - 1);
            await write.ExecuteNonQueryAsync();
        }

        await transaction.CommitAsync();
        Interlocked.Increment(ref sold);
    });

    return sold;
}

static async Task Race(Func<Task> buy)
{
    var gate = new TaskCompletionSource();
    var buyers = Enumerable.Range(0, Buyers).Select(async _ =>
    {
        await gate.Task;

        try
        {
            await buy();
        }
        catch (PostgresException)
        {
            // Aborted by the server. Row two is where this happens.
        }
    }).ToArray();

    gate.SetResult();
    await Task.WhenAll(buyers);
}

async Task Reset() => await Execute(
    $"DELETE FROM stock WHERE sku='W'; INSERT INTO stock (sku, quantity) VALUES ('W', {Stock});");

async Task<int> Quantity()
{
    await using var connection = new NpgsqlConnection(connectionString);
    await connection.OpenAsync();
    await using var command = new NpgsqlCommand("SELECT quantity FROM stock WHERE sku='W'", connection);

    return (int)(await command.ExecuteScalarAsync())!;
}

async Task Execute(string sql)
{
    await using var connection = new NpgsqlConnection(connectionString);
    await connection.OpenAsync();
    await using var command = new NpgsqlCommand(sql, connection);
    await command.ExecuteNonQueryAsync();
}
