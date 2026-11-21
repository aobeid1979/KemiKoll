using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace KemiKoll.Infrastructure.Persistence;

/// <summary>
/// Creates a <see cref="KemiKollDbContext"/> for the <c>dotnet ef</c> command-line tool
/// (creating migrations, updating the database). Not used when the app runs.
/// </summary>
/// <remarks>
/// <para>
/// <c>dotnet ef migrations add</c> does not connect to a database, so the default
/// connection string below is never used for it.
/// </para>
/// <para>
/// <c>dotnet ef database update</c> does connect. Pass the real connection string with
/// <c>--connection</c>, so the password stays out of the code:
/// <code>dotnet ef database update --project src/KemiKoll.Infrastructure --connection "Host=localhost;Port=5432;Database=kemikoll;Username=kemikoll;Password=..."</code>
/// </para>
/// </remarks>
public sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<KemiKollDbContext>
{
    // No password: only good enough for commands that do not connect.
    private const string DefaultConnectionString = "Host=localhost;Port=5432;Database=kemikoll;Username=kemikoll";

    /// <inheritdoc />
    public KemiKollDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<KemiKollDbContext>()
            .UseNpgsql(DefaultConnectionString)
            .Options;

        return new KemiKollDbContext(options);
    }
}
