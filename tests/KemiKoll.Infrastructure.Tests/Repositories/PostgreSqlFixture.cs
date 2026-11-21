using KemiKoll.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;

using Testcontainers.PostgreSql;

namespace KemiKoll.Infrastructure.Tests.Repositories;

/// <summary>
/// Starts one temporary PostgreSQL container (same version as docker-compose.yml) for all
/// repository tests, and creates the tables with the real migrations.
/// Requires Docker: Docker Desktop locally, built in on GitHub Actions.
/// </summary>
public sealed class PostgreSqlFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:17").Build();

    /// <summary>Creates a new context connected to the test database.</summary>
    public KemiKollDbContext CreateContext() =>
        new(new DbContextOptionsBuilder<KemiKollDbContext>()
            .UseNpgsql(_container.GetConnectionString())
            .Options);

    /// <summary>A factory for the repositories, connected to the test database.</summary>
    public IDbContextFactory<KemiKollDbContext> ContextFactory => new TestContextFactory(this);

    public async Task InitializeAsync()
    {
        await _container.StartAsync();

        // Apply the real migrations: this also tests that they run on PostgreSQL.
        await using var context = CreateContext();
        await context.Database.MigrateAsync();
    }

    public async Task DisposeAsync() => await _container.DisposeAsync();

    /// <summary>Removes all rows, so every test starts with an empty database.</summary>
    public async Task ResetAsync()
    {
        await using var context = CreateContext();
        await context.Database.ExecuteSqlRawAsync("TRUNCATE products, safety_data_sheets");
    }

    private sealed class TestContextFactory(PostgreSqlFixture fixture) : IDbContextFactory<KemiKollDbContext>
    {
        public KemiKollDbContext CreateDbContext() => fixture.CreateContext();
    }
}

/// <summary>Lets all repository test classes share one PostgreSQL container.</summary>
[CollectionDefinition(Name)]
public sealed class PostgreSqlTestGroup : ICollectionFixture<PostgreSqlFixture>
{
    public const string Name = "PostgreSQL";
}
