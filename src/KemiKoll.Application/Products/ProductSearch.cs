namespace KemiKoll.Application.Products;

/// <summary>Quick filters above the product list.</summary>
public enum ProductFilter
{
    /// <summary>All products.</summary>
    All,

    /// <summary>Products whose data sheet is missing or due for review.</summary>
    NeedsAttention,

    /// <summary>Products with at least one hazard statement or pictogram.</summary>
    Hazardous,
}

/// <summary>
/// Filters the product list by a search text, for the search box above the list.
/// </summary>
/// <remarks>
/// The text is split into words; a product matches when EVERY word is found in its name,
/// supplier, hazard codes or pictogram codes (part of a word is enough, upper/lower case
/// is ignored). So "aspen h225" finds the Aspen products that have H225.
/// </remarks>
public static class ProductSearch
{
    /// <summary>Returns the products that match the search text and the quick filter, in the same order.</summary>
    /// <param name="products">The products to filter.</param>
    /// <param name="searchText">What the user typed. Empty or blank matches all products.</param>
    /// <param name="filter">The quick filter. <see cref="ProductFilter.All"/> keeps every product.</param>
    /// <returns>The matching products.</returns>
    public static IReadOnlyList<ProductSummary> Filter(
        IReadOnlyList<ProductSummary> products,
        string? searchText,
        ProductFilter filter = ProductFilter.All)
    {
        ArgumentNullException.ThrowIfNull(products);

        var words = (searchText ?? string.Empty).Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
        if (words.Length == 0 && filter == ProductFilter.All)
        {
            return products;
        }

        return products
            .Where(product => IsIn(product, filter) && words.All(word => Matches(product, word)))
            .ToList();
    }

    /// <summary>
    /// <see langword="true"/> if the product's data sheet needs action: missing, or due for review.
    /// </summary>
    /// <param name="product">The product.</param>
    /// <returns>Whether someone should contact the supplier.</returns>
    public static bool NeedsAttention(ProductSummary product)
    {
        ArgumentNullException.ThrowIfNull(product);
        return product.DataSheetStatus is DataSheetStatus.Missing or DataSheetStatus.DueForReview;
    }

    private static bool IsIn(ProductSummary product, ProductFilter filter) => filter switch
    {
        ProductFilter.NeedsAttention => NeedsAttention(product),
        ProductFilter.Hazardous => product.IsHazardous,
        _ => true,
    };

    private static bool Matches(ProductSummary product, string word) =>
        Contains(product.Name, word) ||
        Contains(product.Supplier, word) ||
        product.HazardCodes.Any(code => Contains(code, word)) ||
        product.PictogramCodes.Any(code => Contains(code, word));

    private static bool Contains(string text, string word) => text.Contains(word, StringComparison.OrdinalIgnoreCase);
}
