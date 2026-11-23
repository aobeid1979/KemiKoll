using KemiKoll.Application.Products;

namespace KemiKoll.Application.Exports;

/// <summary>Everything an export file shows: the products and the lines above the table.</summary>
/// <param name="Products">The products, in the order they should appear.</param>
/// <param name="ExportDate">The date printed in the file.</param>
/// <param name="Selection">
/// Which filter was used, for example "Needs attention · Search: aspen",
/// or <see langword="null"/> when every product is included.
/// </param>
public sealed record InventorySheet(IReadOnlyList<ProductSummary> Products, DateOnly ExportDate, string? Selection = null);
