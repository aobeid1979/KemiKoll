using System.Collections.Concurrent;

using KemiKoll.Application.Products;
using KemiKoll.Domain.Products;

namespace KemiKoll.Infrastructure.Products;

/// <summary>
/// Temporary product storage that keeps products in memory.
/// </summary>
/// <remarks>
/// Used until the database is added. All data is lost when the application restarts.
/// It must be registered as a single shared instance (singleton), and it is thread-safe
/// because several users can work at the same time.
/// </remarks>
public sealed class InMemoryProductRepository : IProductRepository
{
    private readonly ConcurrentDictionary<Guid, ChemicalProduct> _products = new();

    /// <inheritdoc />
    /// <exception cref="InvalidOperationException">A product with the same id is already stored.</exception>
    public Task AddAsync(ChemicalProduct product, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(product);

        if (!_products.TryAdd(product.Id, product))
        {
            throw new InvalidOperationException($"A product with id {product.Id} is already stored.");
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<ChemicalProduct>> ListAsync(CancellationToken cancellationToken = default)
    {
        IReadOnlyList<ChemicalProduct> products = [.. _products.Values];
        return Task.FromResult(products);
    }
}
