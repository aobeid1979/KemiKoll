using KemiKoll.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using Npgsql;

namespace KemiKoll.Infrastructure.Tests.Repositories;

[Collection(PostgreSqlTestGroup.Name)]
public sealed class DatabaseCheckTests(PostgreSqlFixture database)
{
    private static Task<DatabaseStatus> CheckAsync(string? connectionString)
    {
        var services = new ServiceCollection().AddInfrastructure(connectionString).BuildServiceProvider();
        return services.CheckDatabaseAsync();
    }

    [Fact]
    public async Task NoConnectionString_IsInMemory()
    {
        var status = await CheckAsync(null);

        Assert.Equal(DatabaseState.InMemory, status.State);
    }

    [Fact]
    public async Task ServerNotRunning_IsUnreachable_WithoutPasswordInMessage()
    {
        // Nothing listens on port 1, like PostgreSQL when Docker is not running.
        var status = await CheckAsync("Host=127.0.0.1;Port=1;Database=kemikoll;Username=kemikoll;Password=secret-123;Timeout=3");

        Assert.Equal(DatabaseState.Unreachable, status.State);
        Assert.Equal("127.0.0.1:1", status.Server);
        Assert.DoesNotContain("secret", status.Server, StringComparison.Ordinal);
    }

    [Fact]
    public async Task MigratedDatabase_IsReady()
    {
        var status = await CheckAsync(database.ConnectionString);

        Assert.Equal(DatabaseState.Ready, status.State);
        Assert.Empty(status.PendingMigrations);
    }

    [Fact]
    public async Task EmptyDatabase_HasPendingMigrations()
    {
        // A new, empty database on the same server, like a fresh install before "dotnet ef database update".
        var databaseName = "kemikoll_empty_" + Guid.NewGuid().ToString("N")[..8];
        await using (var context = database.CreateContext())
        {
#pragma warning disable EF1002 // The name is generated above, not user input.
            await context.Database.ExecuteSqlRawAsync($"CREATE DATABASE {databaseName}");
#pragma warning restore EF1002
        }

        var connectionString = new NpgsqlConnectionStringBuilder(database.ConnectionString) { Database = databaseName }.ConnectionString;

        var status = await CheckAsync(connectionString);

        Assert.Equal(DatabaseState.PendingMigrations, status.State);
        Assert.Contains(status.PendingMigrations, name => name.EndsWith("_InitialCreate", StringComparison.Ordinal));
    }
}
