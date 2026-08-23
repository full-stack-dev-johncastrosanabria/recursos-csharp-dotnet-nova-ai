// The module's real-world case, both halves of it, measured against a real
// socket rather than described.
//
// A loopback server counts the TCP connections it accepts. Nothing here is
// simulated: these are real connections, opened by the real SocketsHttpHandler,
// and the counts are what the operating system actually saw.

using System.Net;
using System.Net.Sockets;
using System.Text;

using var server = new CountingServer();
var address = server.Start();

Console.WriteLine("50 requests to the same address, two ways.");
Console.WriteLine();
Console.WriteLine($"  {"client strategy",-46}{"connections",12}");
Console.WriteLine("  " + new string('-', 60));

await PerRequestClient(address, server);
await SharedClient(address, server);

Console.WriteLine();
Console.WriteLine("A connection pool lives in the HANDLER, not in the HttpClient. A new");
Console.WriteLine("client built the default handler with it, so nothing was ever reused --");
Console.WriteLine("and disposing it did not release the sockets either. A closed TCP");
Console.WriteLine("connection sits in TIME_WAIT for minutes, holding its local port, which");
Console.WriteLine("is how a process runs out of ports while appearing to leak nothing.");
Console.WriteLine();
Console.WriteLine("So the fix everyone reaches for is one static HttpClient. It works, and");
Console.WriteLine("it introduces the second half of this case:");
Console.WriteLine();
Console.WriteLine($"  {"shared client, 3 waves over ~1.2s",-46}{"connections",12}");
Console.WriteLine("  " + new string('-', 60));

await Waves(address, server, "default pooled lifetime (infinite)", Timeout.InfiniteTimeSpan);
await Waves(address, server, "PooledConnectionLifetime = 250ms", TimeSpan.FromMilliseconds(250));

Console.WriteLine();
Console.WriteLine("The first row never opens a second connection. That is the point people");
Console.WriteLine("miss: a pooled connection is bound to the IP address it was opened to,");
Console.WriteLine("and it never asks DNS anything again. Fail the far side over to a new");
Console.WriteLine("address and the pooled connection keeps talking to the old one until");
Console.WriteLine("something closes it -- which, for a healthy connection, is never.");
Console.WriteLine();
Console.WriteLine("PooledConnectionLifetime is the answer: it retires connections on a");
Console.WriteLine("clock whether or not they are healthy, so DNS is consulted again. Two");
Console.WriteLine("minutes is the usual choice, and it is what IHttpClientFactory gives you");
Console.WriteLine("by rotating handlers on the same interval.");
Console.WriteLine();
Console.WriteLine($"Requests served: {server.Requests}. Connections accepted: {server.Connections}.");

static async Task PerRequestClient(Uri address, CountingServer server)
{
    var before = server.Connections;

    for (var request = 0; request < 50; request++)
    {
        using var client = new HttpClient();
        await client.GetStringAsync(address);
    }

    Report("a new HttpClient per request (disposed)", server.Connections - before);
}

static async Task SharedClient(Uri address, CountingServer server)
{
    var before = server.Connections;

    using var client = new HttpClient();
    for (var request = 0; request < 50; request++)
    {
        await client.GetStringAsync(address);
    }

    Report("one shared HttpClient", server.Connections - before);
}

static async Task Waves(Uri address, CountingServer server, string label, TimeSpan pooledLifetime)
{
    var before = server.Connections;

    using var handler = new SocketsHttpHandler { PooledConnectionLifetime = pooledLifetime };
    using var client = new HttpClient(handler);

    for (var wave = 0; wave < 3; wave++)
    {
        await client.GetStringAsync(address);
        await Task.Delay(400);
    }

    Report(label, server.Connections - before);
}

static void Report(string label, int connections)
    => Console.WriteLine($"  {label,-46}{connections,12}");

/// <summary>A minimal HTTP/1.1 server that counts the connections it accepts.</summary>
internal sealed class CountingServer : IDisposable
{
    private readonly TcpListener _listener = new(IPAddress.Loopback, 0);
    private readonly CancellationTokenSource _stopping = new();
    private int _connections;
    private int _requests;

    public int Connections => Volatile.Read(ref _connections);

    public int Requests => Volatile.Read(ref _requests);

    public Uri Start()
    {
        _listener.Start();
        _ = Task.Run(AcceptLoopAsync);

        var port = ((IPEndPoint)_listener.LocalEndpoint).Port;

        return new Uri($"http://127.0.0.1:{port}/");
    }

    public void Dispose()
    {
        _stopping.Cancel();
        _listener.Dispose();
        _stopping.Dispose();
    }

    private async Task AcceptLoopAsync()
    {
        try
        {
            while (!_stopping.IsCancellationRequested)
            {
                var connection = await _listener.AcceptTcpClientAsync(_stopping.Token);
                Interlocked.Increment(ref _connections);
                _ = Task.Run(() => ServeAsync(connection));
            }
        }
        catch (OperationCanceledException)
        {
        }
        catch (SocketException)
        {
        }
    }

    private async Task ServeAsync(TcpClient connection)
    {
        using (connection)
        {
            var stream = connection.GetStream();
            var buffer = new byte[4096];

            // Keep-alive: serve every request that arrives on this one
            // connection, which is exactly what makes reuse observable.
            while (!_stopping.IsCancellationRequested)
            {
                var read = await stream.ReadAsync(buffer);
                if (read == 0)
                {
                    return;
                }

                Interlocked.Increment(ref _requests);

                var response = Encoding.ASCII.GetBytes(
                    "HTTP/1.1 200 OK\r\nContent-Length: 2\r\nConnection: keep-alive\r\n\r\nok");
                await stream.WriteAsync(response);
            }
        }
    }
}
