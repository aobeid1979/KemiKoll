using KemiKoll.Application.Products;
using KemiKoll.Domain.Products;
using KemiKoll.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;

namespace KemiKoll.Infrastructure.Products;

/// <summary>
/// Stores chemical products in PostgreSQL with EF Core.
/// </summary>
/// <remarks>
/// Every method creates its own short-lived <see cref="KemiKollDbContext"/> from the factory.
/// In Blazor Server one user session (circuit) can live for hours, so a single long-lived
/// context would collect stale data and could be used by two operations at once.
/// </remarks>
/// <param name="contextFactory">Creates database contexts.</param>
public sealed class EfProductRepository(IDbContextFactory<KemiKollDbContext> contextFactory) : IProductRepository
{
    /// <inheritdoc />
    public async Task AddAsync(ChemicalProduct product, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(product);

        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        context.Products.Add(product);
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ChemicalProduct>> ListAsync(CancellationToken cancellationToken = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

        // Read-only: no change tracking needed.
        return await context.Products.AsNoTracking().ToListAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<ChemicalProduct?> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        return await context.Products.AsNoTracking()
            .SingleOrDefaultAsync(product => product.Id == id, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<bool> UpdateAsync(ChemicalProduct product, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(product);

        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

        // The product was loaded by another context, so it is attached here and all its
        // columns are written.
        context.Products.Update(product);
        try
        {
            await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return true;
        }
        catch (DbUpdateConcurrencyException)
        {
            // No row was updated: the product was deleted in the meantime.
            return false;
        }
    }

    /// <inheritdoc />
    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

        // One DELETE statement; the database deletes the product's data sheets too (cascade).
        var deleted = await context.Products
            .Where(product => product.Id == id)
            .ExecuteDeleteAsync(cancellationToken)
            .ConfigureAwait(false);
        return deleted > 0;
    }
}
