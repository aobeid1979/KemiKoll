using KemiKoll.Application.SafetyDataSheets;
using KemiKoll.Domain.SafetyDataSheets;

namespace KemiKoll.Application.Tests.TestDoubles;

/// <summary>A minimal data sheet repository for tests: a plain list.</summary>
internal sealed class FakeSafetyDataSheetRepository : ISafetyDataSheetRepository
{
    public List<SafetyDataSheet> Sheets { get; } = [];

    public Task AddAsync(SafetyDataSheet sheet, CancellationToken cancellationToken = default)
    {
        Sheets.Add(sheet);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<SafetyDataSheet>> ListAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<SafetyDataSheet>>(Sheets);

    public Task DeleteForProductAsync(Guid productId, CancellationToken cancellationToken = default)
    {
        Sheets.RemoveAll(sheet => sheet.ProductId == productId);
        return Task.CompletedTask;
    }
}
