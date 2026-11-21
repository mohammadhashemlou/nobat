using Npgsql;

namespace Nobat.Api.Tests.Infrastructure;

/// <summary>
/// Manages throw-away PostgreSQL databases for integration tests.
/// A dedicated database per test run keeps the developer's dev database untouched
/// and makes the suite safe to run repeatedly.
/// </summary>
public static class TestDatabase
{
    private static readonly Lazy<(bool Available, string Reason)> Probe = new(ProbeOnce);

    public static bool IsAvailable => Probe.Value.Available;

    public static string UnavailableReason => Probe.Value.Reason;

    /// <summary>
    /// Base connection string, resolved from the environment so CI can point at its own
    /// service container: <c>NOBAT_TEST_DB</c> wins, then the app's own connection string.
    /// </summary>
    public static string BaseConnectionString =>
        Environment.GetEnvironmentVariable("NOBAT_TEST_DB")
        ?? Environment.GetEnvironmentVariable("ConnectionStrings__Default")
        ?? "Host=localhost;Port=5433;Database=nobat;Username=nobat;Password=nobat_dev_password";

    public static string BuildConnectionString(string database)
        => new NpgsqlConnectionStringBuilder(BaseConnectionString)
        {
            Database = database,
            Timeout = 15,
            Pooling = false
        }.ConnectionString;

    public static void Create(string database)
    {
        Execute($"CREATE DATABASE \"{database}\"");
    }

    public static void Drop(string database)
    {
        try
        {
            // FORCE terminates leftover pooled connections (PostgreSQL 13+).
            Execute($"DROP DATABASE IF EXISTS \"{database}\" WITH (FORCE)");
        }
        catch (NpgsqlException)
        {
            // Best-effort cleanup: a stray connection must never fail the test run.
        }
    }

    private static void Execute(string sql)
    {
        var admin = new NpgsqlConnectionStringBuilder(BaseConnectionString)
        {
            Database = "postgres",
            Timeout = 15,
            Pooling = false
        };

        using var connection = new NpgsqlConnection(admin.ConnectionString);
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.ExecuteNonQuery();
    }

    private static (bool, string) ProbeOnce()
    {
        try
        {
            Execute("SELECT 1");
            return (true, string.Empty);
        }
        catch (Exception ex)
        {
            return (false,
                $"PostgreSQL is not reachable ({ex.GetType().Name}: {ex.Message}). " +
                "Integration tests need a PostgreSQL server; set NOBAT_TEST_DB to a connection string " +
                "whose user may CREATE DATABASE.");
        }
    }
}
