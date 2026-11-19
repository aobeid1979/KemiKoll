using KemiKoll.Domain.SafetyDataSheets;

namespace KemiKoll.Application.SafetyDataSheets;

/// <summary>
/// Stores and loads safety data sheets.
/// </summary>
/// <remarks>
/// Like <c>IProductRepository</c>, the implementation lives in the Infrastructure layer.
/// </remarks>
public interface ISafetyDataSheetRepository
{
    /// <summary>Saves a new safety data sheet.</summary>
    /// <param name="sheet">The data sheet to save.</param>
    /// <param name="cancellationToken">Cancels the operation.</param>
    /// <returns>A task that completes when the data sheet is saved.</returns>
    Task AddAsync(SafetyDataSheet sheet, CancellationToken cancellationToken = default);

    /// <summary>Loads all safety data sheets.</summary>
    /// <param name="cancellationToken">Cancels the operation.</param>
    /// <returns>All saved data sheets, in no particular order.</returns>
    Task<IReadOnlyList<SafetyDataSheet>> ListAsync(CancellationToken cancellationToken = default);
}
