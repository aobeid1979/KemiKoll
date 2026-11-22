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

    public Task<ChemicalProduct?> GetAsync(Guid id, CancellationToken cancellationToken = default) =>
        Task.FromResult(Products.SingleOrDefault(product => product.Id == id));

    public int UpdateCount { get; private set; }

    public Task<bool> UpdateAsync(ChemicalProduct product, CancellationToken cancellationToken = default)
    {
        UpdateCount++;
        return Task.FromResult(Products.Any(p => p.Id == product.Id));
    }

    public Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default) =>
        Task.FromResult(Products.RemoveAll(product => product.Id == id) > 0);
}
