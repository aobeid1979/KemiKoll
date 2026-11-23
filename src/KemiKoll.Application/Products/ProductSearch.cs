namespace KemiKoll.Application.Products;

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
    /// <summary>Returns the products that match the search text, in the same order.</summary>
    /// <param name="products">The products to filter.</param>
    /// <param name="searchText">What the user typed. Empty or blank returns all products.</param>
    /// <returns>The matching products.</returns>
    public static IReadOnlyList<ProductSummary> Filter(IReadOnlyList<ProductSummary> products, string? searchText)
    {
        ArgumentNullException.ThrowIfNull(products);

        var words = (searchText ?? string.Empty).Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
        if (words.Length == 0)
        {
            return products;
        }

        return products.Where(product => words.All(word => Matches(product, word))).ToList();
    }

    private static bool Matches(ProductSummary product, string word) =>
        Contains(product.Name, word) ||
        Contains(product.Supplier, word) ||
        product.HazardCodes.Any(code => Contains(code, word)) ||
        product.PictogramCodes.Any(code => Contains(code, word));

    private static bool Contains(string text, string word) => text.Contains(word, StringComparison.OrdinalIgnoreCase);
}
