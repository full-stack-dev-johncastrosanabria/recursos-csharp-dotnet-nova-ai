// Why the jitter in exercise 2 is not decoration.
//
// This one needs no database. When a group of transactions collide, they are
// by definition doing the same thing at the same moment -- so if they all back
// off by the same amount, they wake at the same moment too, and collide again.
// Jitter is what turns a synchronised crowd into a queue.
//
// The simulation below is deliberately simple: N clients collide, back off, and
// retry. A round in which more than one client is awake is a round in which
// they collide again.

const int Clients = 24;
const int Attempts = 5;
const int BaseDelayMs = 50;

Console.WriteLine($"{Clients} clients collide at t=0, then retry with exponential backoff.");
Console.WriteLine();
Console.WriteLine($"  {"backoff",-22}{"distinct wake-up times",26}{"worst pile-up",16}");
Console.WriteLine("  " + new string('-', 66));

Report("no jitter", _ => 1.0);
Report("jitter, 0.5x - 1.5x", random => 0.5 + random.NextDouble());

Console.WriteLine();
Console.WriteLine("Without jitter every client computes the same delay from the same start,");
Console.WriteLine("so all 24 wake in the same instant on every round. The contention that");
Console.WriteLine("caused the first collision is reproduced exactly, and the retry loop is");
Console.WriteLine("not recovering from the pile-up -- it is rebuilding it, on a timer.");
Console.WriteLine();
Console.WriteLine("With jitter the same 24 clients spread across the window, and each round");
Console.WriteLine("spreads them further. Nothing else changed: same base delay, same doubling,");
Console.WriteLine("same number of attempts.");
Console.WriteLine();
Console.WriteLine("This is the same failure as a retry storm against an HTTP dependency in");
Console.WriteLine("module 08, and module 19 treats it properly with budgets and circuit");
Console.WriteLine("breakers. The reason it belongs here too is that a serialization failure");
Console.WriteLine("is BY CONSTRUCTION a collision between transactions that wanted the same");
Console.WriteLine("row at the same time. They are the most synchronised group you will ever");
Console.WriteLine("retry, so they are the group jitter matters most for.");

static void Report(string label, Func<Random, double> jitter)
{
    var random = new Random(Seed: 20260823);
    var wakeUps = new List<int>();

    for (var client = 0; client < Clients; client++)
    {
        var elapsed = 0.0;

        for (var attempt = 1; attempt <= Attempts; attempt++)
        {
            elapsed += BaseDelayMs * Math.Pow(2, attempt - 1) * jitter(random);
            wakeUps.Add((int)elapsed);
        }
    }

    // Group wake-ups into 1ms buckets: anything sharing a bucket collides.
    var buckets = wakeUps.GroupBy(millisecond => millisecond).ToArray();

    Console.WriteLine($"  {label,-22}{buckets.Length,26}{buckets.Max(bucket => bucket.Count()),16}");
}
