using KemiKoll.Application.SafetyDataSheets;
using KemiKoll.Domain.Hazards;
using KemiKoll.Domain.SafetyDataSheets;

namespace KemiKoll.Application.Products;

/// <summary>
/// The state of a product's safety data sheet, as shown in the product list.
/// </summary>
public enum DataSheetStatus
{
    /// <summary>The product is not hazardous and has no data sheet. Nothing to do.</summary>
    NotRequired,

    /// <summary>The product is hazardous but has no data sheet. Ask the supplier for one.</summary>
    Missing,

    /// <summary>The latest data sheet is younger than the review age.</summary>
    UpToDate,

    /// <summary>The latest data sheet is older than the review age. Check with the supplier for a newer version.</summary>
    DueForReview,
}

/// <summary>
/// One row in the product list, ready to show on screen.
/// </summary>
/// <remarks>
/// Screens receive this simple record instead of the <c>ChemicalProduct</c> entity,
/// so they cannot change a product by accident and do not depend on the domain model.
/// </remarks>
/// <param name="Id">The product id.</param>
/// <param name="Name">The trade name.</param>
/// <param name="Supplier">The supplier.</param>
/// <param name="HazardCodes">Hazard statement codes, for example "H225".</param>
/// <param name="PictogramCodes">GHS pictogram codes, for example "GHS02".</param>
/// <param name="IsHazardous">Whether the product has any hazard statement or pictogram.</param>
/// <param name="DataSheetStatus">The state of the product's safety data sheet.</param>
/// <param name="DataSheetRevisionDate">Revision date of the latest data sheet, or <see langword="null"/> if there is none.</param>
public sealed record ProductSummary(
    Guid Id,
    string Name,
    string Supplier,
    IReadOnlyList<string> HazardCodes,
    IReadOnlyList<string> PictogramCodes,
    bool IsHazardous,
    DataSheetStatus DataSheetStatus,
    DateOnly? DataSheetRevisionDate);

/// <summary>
/// Use case: list all chemical products in the inventory, with the state of their data sheets.
/// </summary>
/// <param name="products">Where products are stored.</param>
/// <param name="sheets">Where safety data sheets are stored.</param>
/// <param name="timeProvider">Gives today's date. Injected so tests can use a fixed date.</param>
public sealed class ListProducts(
    IProductRepository products,
    ISafetyDataSheetRepository sheets,
    TimeProvider timeProvider)
{
    /// <summary>Loads all products, sorted by name (case-insensitive).</summary>
    /// <param name="cancellationToken">Cancels the operation.</param>
    /// <returns>One summary per product.</returns>
    public async Task<IReadOnlyList<ProductSummary>> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        var allProducts = await products.ListAsync(cancellationToken).ConfigureAwait(false);
        var allSheets = await sheets.ListAsync(cancellationToken).ConfigureAwait(false);

        // A product can have several revisions over time; only the newest one counts.
        var latestRevisionByProduct = allSheets
            .GroupBy(sheet => sheet.ProductId)
            .ToDictionary(group => group.Key, group => group.MaxBy(sheet => sheet.RevisionDate)!);

        var today = DateOnly.FromDateTime(timeProvider.GetLocalNow().DateTime);

        // There are no company accounts yet, so every product uses the default policy (3 years).
        // When companies are added, the company's own ReviewPolicy is used here instead.
        var policy = ReviewPolicy.Default;

        return allProducts
            .OrderBy(product => product.Name, StringComparer.OrdinalIgnoreCase)
            .Select(product =>
            {
                var latestSheet = latestRevisionByProduct.GetValueOrDefault(product.Id);
                return new ProductSummary(
                    product.Id,
                    product.Name,
                    product.Supplier,
                    product.HazardStatements.Select(statement => statement.Code).ToList(),
                    product.Pictograms.Select(pictogram => pictogram.ToCode()).ToList(),
                    product.IsHazardous,
                    GetStatus(product.IsHazardous, latestSheet, today, policy),
                    latestSheet?.RevisionDate);
            })
            .ToList();
    }

    private static DataSheetStatus GetStatus(bool isHazardous, SafetyDataSheet? latestSheet, DateOnly today, ReviewPolicy policy)
    {
        if (latestSheet is null)
        {
            return isHazardous ? DataSheetStatus.Missing : DataSheetStatus.NotRequired;
        }

        return latestSheet.IsDueForReview(today, policy) ? DataSheetStatus.DueForReview : DataSheetStatus.UpToDate;
    }
}
