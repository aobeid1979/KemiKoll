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
}
