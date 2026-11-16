using KemiKoll.Application.Products;
using KemiKoll.Domain.Products;

namespace KemiKoll.Application.Tests.Products;

/// <summary>
/// A minimal repository for tests: a plain list, so tests can check what was saved.
/// (The test project may not reference Infrastructure — see the architecture tests.)
/// </summary>
internal sealed class FakeProductRepository : IProductRepository
{
    public List<ChemicalProduct> Products { get; } = [];

    public Task AddAsync(ChemicalProduct product, CancellationToken cancellationToken = default)
    {
        Products.Add(product);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<ChemicalProduct>> ListAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<ChemicalProduct>>(Products);
}
