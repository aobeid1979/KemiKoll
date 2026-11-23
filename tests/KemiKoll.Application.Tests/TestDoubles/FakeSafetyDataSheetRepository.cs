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

    public Task<IReadOnlyList<SafetyDataSheet>> ListForProductAsync(Guid productId, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<SafetyDataSheet>>(Sheets.Where(sheet => sheet.ProductId == productId).ToList());

    public Task<SafetyDataSheet?> GetAsync(Guid id, CancellationToken cancellationToken = default) =>
        Task.FromResult(Sheets.SingleOrDefault(sheet => sheet.Id == id));

    public int UpdateCount { get; private set; }

    public Task<bool> UpdateAsync(SafetyDataSheet sheet, CancellationToken cancellationToken = default)
    {
        UpdateCount++;
        return Task.FromResult(Sheets.Any(s => s.Id == sheet.Id));
    }

    public Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default) =>
        Task.FromResult(Sheets.RemoveAll(sheet => sheet.Id == id) > 0);
}
