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
}
