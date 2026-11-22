using System.Collections.Concurrent;

using KemiKoll.Application.SafetyDataSheets;
using KemiKoll.Domain.SafetyDataSheets;

namespace KemiKoll.Infrastructure.SafetyDataSheets;

/// <summary>
/// Temporary safety data sheet storage that keeps data sheets in memory.
/// </summary>
/// <remarks>
/// Used until the database is added. All data is lost when the application restarts.
/// Register it as a singleton; it is thread-safe.
/// </remarks>
public sealed class InMemorySafetyDataSheetRepository : ISafetyDataSheetRepository
{
    private readonly ConcurrentDictionary<Guid, SafetyDataSheet> _sheets = new();

    /// <inheritdoc />
    /// <exception cref="InvalidOperationException">A data sheet with the same id is already stored.</exception>
    public Task AddAsync(SafetyDataSheet sheet, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(sheet);

        if (!_sheets.TryAdd(sheet.Id, sheet))
        {
            throw new InvalidOperationException($"A data sheet with id {sheet.Id} is already stored.");
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<SafetyDataSheet>> ListAsync(CancellationToken cancellationToken = default)
    {
        IReadOnlyList<SafetyDataSheet> sheets = [.. _sheets.Values];
        return Task.FromResult(sheets);
    }

    /// <inheritdoc />
    public Task DeleteForProductAsync(Guid productId, CancellationToken cancellationToken = default)
    {
        foreach (var sheet in _sheets.Values.Where(sheet => sheet.ProductId == productId))
        {
            _sheets.TryRemove(sheet.Id, out _);
        }

        return Task.CompletedTask;
    }
}
