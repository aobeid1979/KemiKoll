namespace KemiKoll.Web;

/// <summary>
/// Log messages written while the app starts.
/// </summary>
/// <remarks>
/// [LoggerMessage] lets the compiler generate the logging code, which is faster than
/// calling LogInformation directly (analyzer rule CA1848).
/// </remarks>
internal static partial class StartupLog
{
    /// <summary>Logs which storage the app uses: PostgreSQL or in memory.</summary>
    /// <param name="logger">The logger to write to.</param>
    /// <param name="storage">A short description of the storage.</param>
    [LoggerMessage(Level = LogLevel.Information, Message = "Storage: {Storage}")]
    public static partial void Storage(ILogger logger, string storage);

    /// <summary>Logs that the database server cannot be reached, with the fix.</summary>
    /// <param name="logger">The logger to write to.</param>
    /// <param name="server">"host:port" of the database server.</param>
    [LoggerMessage(
        Level = LogLevel.Critical,
        Message = "Cannot reach the PostgreSQL database at {Server}. Is Docker running? Start it with: docker compose up -d")]
    public static partial void DatabaseUnreachable(ILogger logger, string server);

    /// <summary>Logs that the database is missing migrations, with the fix.</summary>
    /// <param name="logger">The logger to write to.</param>
    /// <param name="migrations">The names of the missing migrations.</param>
    [LoggerMessage(
        Level = LogLevel.Critical,
        Message = "The database is missing these migrations: {Migrations}. Update it with: dotnet ef database update --project src/KemiKoll.Infrastructure --connection \"<your connection string>\"")]
    public static partial void PendingMigrations(ILogger logger, string migrations);
}
