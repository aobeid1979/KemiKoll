using KemiKoll.Domain.Products;

namespace KemiKoll.Application.Products;

/// <summary>
/// Stores and loads chemical products.
/// </summary>
/// <remarks>
/// The Application layer only knows this interface. The real storage (in memory today,
/// a database later) lives in the Infrastructure layer, so it can be replaced
/// without changing any use case or screen.
/// </remarks>
public interface IProductRepository
{
    /// <summary>Saves a new product.</summary>
    /// <param name="product">The product to save.</param>
    /// <param name="cancellationToken">Cancels the operation.</param>
    /// <returns>A task that completes when the product is saved.</returns>
    Task AddAsync(ChemicalProduct product, CancellationToken cancellationToken = default);

    /// <summary>Loads all products.</summary>
    /// <param name="cancellationToken">Cancels the operation.</param>
    /// <returns>All saved products, in no particular order.</returns>
    Task<IReadOnlyList<ChemicalProduct>> ListAsync(CancellationToken cancellationToken = default);

    /// <summary>Loads one product.</summary>
    /// <param name="id">The product id.</param>
    /// <param name="cancellationToken">Cancels the operation.</param>
    /// <returns>The product, or <see langword="null"/> if it does not exist.</returns>
    Task<ChemicalProduct?> GetAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Saves the changes to an existing product.</summary>
    /// <param name="product">The changed product.</param>
    /// <param name="cancellationToken">Cancels the operation.</param>
    /// <returns><see langword="true"/> if saved; <see langword="false"/> if the product no longer exists.</returns>
    Task<bool> UpdateAsync(ChemicalProduct product, CancellationToken cancellationToken = default);

    /// <summary>Deletes a product together with its safety data sheets.</summary>
    /// <param name="id">The product id.</param>
    /// <param name="cancellationToken">Cancels the operation.</param>
    /// <returns><see langword="true"/> if deleted; <see langword="false"/> if it did not exist.</returns>
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
