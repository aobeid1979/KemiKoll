using KemiKoll.Domain.Hazards;

namespace KemiKoll.Application.Products;

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
public sealed record ProductSummary(
    Guid Id,
    string Name,
    string Supplier,
    IReadOnlyList<string> HazardCodes,
    IReadOnlyList<string> PictogramCodes,
    bool IsHazardous);

/// <summary>
/// Use case: list all chemical products in the inventory.
/// </summary>
/// <param name="repository">Where products are stored.</param>
public sealed class ListProducts(IProductRepository repository)
{
    /// <summary>Loads all products, sorted by name (case-insensitive).</summary>
    /// <param name="cancellationToken">Cancels the operation.</param>
    /// <returns>One summary per product.</returns>
    public async Task<IReadOnlyList<ProductSummary>> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        var products = await repository.ListAsync(cancellationToken).ConfigureAwait(false);

        return products
            .OrderBy(product => product.Name, StringComparer.OrdinalIgnoreCase)
            .Select(product => new ProductSummary(
                product.Id,
                product.Name,
                product.Supplier,
                product.HazardStatements.Select(statement => statement.Code).ToList(),
                product.Pictograms.Select(pictogram => pictogram.ToCode()).ToList(),
                product.IsHazardous))
            .ToList();
    }
}
