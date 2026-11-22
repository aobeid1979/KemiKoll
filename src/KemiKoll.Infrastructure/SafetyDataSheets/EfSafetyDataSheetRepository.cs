using KemiKoll.Application.SafetyDataSheets;
using KemiKoll.Domain.SafetyDataSheets;
using KemiKoll.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;

namespace KemiKoll.Infrastructure.SafetyDataSheets;

/// <summary>
/// Stores safety data sheets in PostgreSQL with EF Core.
/// </summary>
/// <remarks>
/// Uses one short-lived context per method, for the same reason as <c>EfProductRepository</c>.
/// </remarks>
/// <param name="contextFactory">Creates database contexts.</param>
public sealed class EfSafetyDataSheetRepository(IDbContextFactory<KemiKollDbContext> contextFactory) : ISafetyDataSheetRepository
{
    /// <inheritdoc />
    public async Task AddAsync(SafetyDataSheet sheet, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(sheet);

        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        context.SafetyDataSheets.Add(sheet);
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<SafetyDataSheet>> ListAsync(CancellationToken cancellationToken = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        return await context.SafetyDataSheets.AsNoTracking().ToListAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task DeleteForProductAsync(Guid productId, CancellationToken cancellationToken = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        await context.SafetyDataSheets
            .Where(sheet => sheet.ProductId == productId)
            .ExecuteDeleteAsync(cancellationToken)
            .ConfigureAwait(false);
    }
}
