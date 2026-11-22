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

    /// <summary>Deletes all safety data sheets of a product.</summary>
    /// <param name="productId">The product id.</param>
    /// <param name="cancellationToken">Cancels the operation.</param>
    /// <returns>A task that completes when the data sheets are deleted.</returns>
    Task DeleteForProductAsync(Guid productId, CancellationToken cancellationToken = default);

    /// <summary>Loads the safety data sheets of one product.</summary>
    /// <param name="productId">The product id.</param>
    /// <param name="cancellationToken">Cancels the operation.</param>
    /// <returns>The product's data sheets, in no particular order.</returns>
    Task<IReadOnlyList<SafetyDataSheet>> ListForProductAsync(Guid productId, CancellationToken cancellationToken = default);

    /// <summary>Loads one safety data sheet.</summary>
    /// <param name="id">The data sheet id.</param>
    /// <param name="cancellationToken">Cancels the operation.</param>
    /// <returns>The data sheet, or <see langword="null"/> if it does not exist.</returns>
    Task<SafetyDataSheet?> GetAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Saves the changes to an existing safety data sheet.</summary>
    /// <param name="sheet">The changed data sheet.</param>
    /// <param name="cancellationToken">Cancels the operation.</param>
    /// <returns><see langword="true"/> if saved; <see langword="false"/> if it no longer exists.</returns>
    Task<bool> UpdateAsync(SafetyDataSheet sheet, CancellationToken cancellationToken = default);

    /// <summary>Deletes one safety data sheet.</summary>
    /// <param name="id">The data sheet id.</param>
    /// <param name="cancellationToken">Cancels the operation.</param>
    /// <returns><see langword="true"/> if deleted; <see langword="false"/> if it did not exist.</returns>
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
