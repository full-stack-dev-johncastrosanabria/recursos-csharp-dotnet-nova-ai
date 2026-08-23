using DotNet.Testcontainers.Builders;
using Microsoft.Data.SqlClient;
using Testcontainers.MsSql;

namespace Training.Module09.IntegrationTests;

/// <summary>
/// A real SQL Server engine, as Azure SQL Edge, seeded with the same orders
/// table the PostgreSQL fixture uses.
///
/// Two notes on the container. The image is azure-sql-edge because it is the
/// one that runs natively on arm64 as well as x64, which the full SQL Server
/// image does not -- and it is the same database engine underneath. It also
/// ships without sqlcmd, so the default readiness probe cannot be used and
/// this waits for the port and then for a connection to actually open.
/// </summary>
public sealed class SqlServerOrders : IAsyncLifetime
{
    private const string Password = "Training!Passw0rd1";

    private readonly MsSqlContainer _container = new MsSqlBuilder("mcr.microsoft.com/azure-sql-edge:latest")
        .WithPassword(Password)
        .WithEnvironment("ACCEPT_EULA", "1")
        .WithEnvironment("MSSQL_SA_PASSWORD", Password)
        .WithWaitStrategy(Wait.ForUnixContainer().UntilInternalTcpPortIsAvailable(1433))
        .Build();

    private string ConnectionString => _container.GetConnectionString();

    public async ValueTask InitializeAsync()
    {
        await _container.StartAsync();
        await WaitForLoginAsync();

        await ExecuteAsync("""
            CREATE TABLE dbo.orders (
              id             int IDENTITY(1,1) PRIMARY KEY,
              customer_email nvarchar(200) NOT NULL,
              status         nvarchar(20)  NOT NULL,
              placed_at      datetime2     NOT NULL,
              total_cents    int           NOT NULL
            );
            """);

        await ExecuteAsync("""
            WITH n AS (SELECT TOP (50000) ROW_NUMBER() OVER (ORDER BY (SELECT NULL)) AS i
                       FROM sys.all_objects a CROSS JOIN sys.all_objects b)
            INSERT INTO dbo.orders (customer_email, status, placed_at, total_cents)
            SELECT CONCAT('User', i % 12500, '@Example.com'),
                   CHOOSE(1 + (i % 4), 'placed', 'paid', 'shipped', 'cancelled'),
                   DATEADD(day, i % 730, '2025-01-01'),
                   100 + (i % 90000)
            FROM n;
            """);

        await ExecuteAsync("CREATE INDEX orders_customer_email_idx ON dbo.orders (customer_email);");
        await ExecuteAsync("CREATE INDEX orders_placed_at_idx ON dbo.orders (placed_at);");
    }

    public ValueTask DisposeAsync() => _container.DisposeAsync();

    /// <summary>Runs a query with SET STATISTICS XML ON and returns the actual plan.</summary>
    public async Task<string> ActualPlanAsync(string sql, CancellationToken cancellationToken)
    {
        await using var connection = new SqlConnection(ConnectionString);
        await connection.OpenAsync(cancellationToken);

        await using (var on = new SqlCommand("SET STATISTICS XML ON", connection))
        {
            await on.ExecuteNonQueryAsync(cancellationToken);
        }

        await using var command = new SqlCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        string? plan = null;
        do
        {
            while (await reader.ReadAsync(cancellationToken))
            {
                if (reader.FieldCount == 1 && reader.GetFieldType(0) == typeof(string))
                {
                    var value = reader.GetString(0);
                    if (value.Contains("ShowPlanXML", StringComparison.Ordinal))
                    {
                        plan = value;
                    }
                }
            }
        }
        while (await reader.NextResultAsync(cancellationToken));

        return plan ?? throw new InvalidOperationException("SQL Server returned no execution plan.");
    }

    public async Task ExecuteAsync(string sql, CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(ConnectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = new SqlCommand(sql, connection) { CommandTimeout = 300 };
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    // The port opens before the engine finishes recovery, so the port alone is
    // not readiness. Keep trying to log in until it works.
    private async Task WaitForLoginAsync()
    {
        var deadline = DateTime.UtcNow.AddMinutes(3);

        while (true)
        {
            try
            {
                await using var connection = new SqlConnection(ConnectionString);
                await connection.OpenAsync();

                return;
            }
            catch (SqlException) when (DateTime.UtcNow < deadline)
            {
                await Task.Delay(TimeSpan.FromSeconds(2));
            }
        }
    }
}
