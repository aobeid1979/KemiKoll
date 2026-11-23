using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using Npgsql;

namespace KemiKoll.Infrastructure.Persistence;

/// <summary>The result of checking the database at startup.</summary>
public enum DatabaseState
{
    /// <summary>No database is configured; data is kept in memory.</summary>
    InMemory,

    /// <summary>The database can be reached and has all migrations.</summary>
    Ready,

    /// <summary>The database server cannot be reached (for example Docker is not running).</summary>
    Unreachable,

    /// <summary>The database can be reached, but some migrations have not been applied yet.</summary>
    PendingMigrations,
}

/// <summary>
/// What the startup check found.
/// </summary>
/// <param name="State">The overall result.</param>
/// <param name="Server">"host:port" of the database server, without user name or password; empty in memory.</param>
/// <param name="PendingMigrations">Names of migrations not yet applied; empty unless <see cref="DatabaseState.PendingMigrations"/>.</param>
public sealed record DatabaseStatus(DatabaseState State, string Server, IReadOnlyList<string> PendingMigrations);

/// <summary>
/// Checks at startup that the database can be used, so a problem is reported once with a clear
/// message instead of as a long error on the first page that needs data.
/// </summary>
public static class DatabaseCheck
{
    /// <summary>Checks the connection and the migrations of the configured database.</summary>
    /// <param name="services">The application's services, after <c>AddInfrastructure</c>.</param>
    /// <param name="cancellationToken">Cancels the check.</param>
    /// <returns>The database state, the server and any missing migrations.</returns>
    public static async Task<DatabaseStatus> CheckDatabaseAsync(this IServiceProvider services, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(services);

        var factory = services.GetService<IDbContextFactory<KemiKollDbContext>>();
        if (factory is null)
        {
            return new DatabaseStatus(DatabaseState.InMemory, string.Empty, []);
        }

        await using var context = await factory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        var server = DescribeServer(context.Database.GetConnectionString());

        // CanConnectAsync returns false (it does not throw) when the server or the database is missing.
        if (!await context.Database.CanConnectAsync(cancellationToken).ConfigureAwait(false))
        {
            return new DatabaseStatus(DatabaseState.Unreachable, server, []);
        }

        var pending = (await context.Database.GetPendingMigrationsAsync(cancellationToken).ConfigureAwait(false)).ToList();
        return pending.Count > 0
            ? new DatabaseStatus(DatabaseState.PendingMigrations, server, pending)
            : new DatabaseStatus(DatabaseState.Ready, server, []);
    }

    // Only host and port: the connection string also contains the password, which must never be logged.
    private static string DescribeServer(string? connectionString)
    {
        var builder = new NpgsqlConnectionStringBuilder(connectionString);
        return $"{builder.Host}:{builder.Port}";
    }
}
